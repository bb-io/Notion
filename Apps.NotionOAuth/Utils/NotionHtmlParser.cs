using System.Text;
using System.Web;
using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Models;
using Apps.NotionOAuth.Models.Request.Page;
using Blackbird.Applications.Sdk.Utils.Extensions.System;
using Blackbird.Applications.Sdk.Utils.Html.Extensions;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.NotionOAuth.Utils;

public static class NotionHtmlParser
{
    private const string AnnotationsAttr = "data-style";
    private const string TypeAttr = "data-type";
    private const string ContentParamsAttr = "data-content-params";
    private const string UntranslatableContentAttr = "data-untranslatable";
    private const string UntranslatableType = "untranslatable";
    private const string ChildBlockIds = "child_block_ids";
    private const string BlockIdAttr = "data-block-id";
    private const string ParentBlockIdAttr = "data-parent-page-id";
    private const string ChildBlockIdsAttr = "data-child-block-ids";
    private const string BlackbirdPageIdAttr = "blackbird-page-id";

    private static readonly string[] UnparsableTypes =
        { "child_database", "unsupported", "file", "audio", "link_preview" };

    private static readonly string[] TextTypes = { "rich_text", "text" };

    public static JObject[] ParseHtml(string html)
    {
        var htmlDoc = html.AsHtmlDocument();
        var translatableNodes = htmlDoc.DocumentNode.SelectSingleNode("/html/body")
            .ChildNodes
            .Where(x => x.Name != "#text")
            .ToArray();

        var jObjects = translatableNodes.Select(MapNodeToBlockChild).ToList();

        var extractedPageId = ExtractPageId(html);
        if (string.IsNullOrEmpty(extractedPageId))
            throw new Exception("Page ID not found in HTML");
        extractedPageId = NormalizeId(extractedPageId);

        var blocksById = jObjects.Where(x => x["id"] != null)
            .ToDictionary(x => NormalizeId(x["id"]!.ToString()));

        foreach (var block in jObjects)
        {
            var type = block["type"]?.ToString();
            if (type == "child_page")
            {
                SetChildPageProperties(block);
            }
        }

        for (int i = jObjects.Count - 1; i >= 0; i--)
        {
            var block = jObjects[i];
            var parentId = block.GetParentId();
            if (parentId != null)
            {
                parentId = NormalizeId(parentId);
                if (extractedPageId != parentId)
                {
                    // The block is not a root block, find its parent
                    if (blocksById.TryGetValue(parentId, out var parentBlock))
                    {
                        AddBlockToParent(block, parentBlock);
                        jObjects.RemoveAt(i);
                    }
                    else
                    {
                        throw new Exception($"Parent block with ID {parentId} not found");
                    }
                }
            }
        }

        // Remove unnecessary properties, otherwise the API will return an error
        jObjects.ForEach(RemoveUnnecessaryProperties);
        return jObjects.ToArray();
    }
    
    private static void SetChildPageProperties(JObject block)
    {
        var title = block["child_page"]?["title"]?.ToString() ?? "Untitled";
        var titleProperty = new TitlePropertyModel
        {
            Title =
            [
                new()
                {
                    Text = new TextContentModel
                    {
                        Content = title
                    }
                }
            ]
        };

        var titlePropertyJson = JObject.Parse(JsonConvert.SerializeObject(titleProperty));
        block["properties"] = titlePropertyJson;
    }

    private static void AddBlockToParent(JObject block, JObject parentBlock)
    {
        var parentType = parentBlock["type"]?.ToString();
        JObject parentContainer;

        if (parentType == "child_page")
        {
            // For 'child_page', add 'children' directly under the parent block
            parentContainer = parentBlock;
        }
        else
        {
            // For other types, 'children' are under parentBlock[parentType]
            parentContainer = parentBlock[parentType] as JObject;
        }

        if (parentContainer == null)
        {
            throw new Exception($"Parent container not found for block type {parentType}");
        }

        if (parentContainer["children"] == null)
        {
            parentContainer["children"] = new JArray { block };
        }
        else
        {
            var childrenArray = (JArray)parentContainer["children"];
            childrenArray.Insert(0, block);
        }
    }

    public static string? GetParentId(this JObject block)
    {
        var parent = block["parent"];
        if (parent != null)
        {
            if (parent["type"]?.ToString() == "page_id")
            {
                return parent["page_id"]?.ToString();
            }
            else if (parent["type"]?.ToString() == "block_id")
            {
                return parent["block_id"]?.ToString();
            }
        }

        return null;
    }

    private static string NormalizeId(string id)
    {
        return id.Replace("-", "").ToLower();
    }
    
    private static void RemoveUnnecessaryProperties(JObject block)
    {
        block.Remove("id");
        block.Remove("object");
        block.Remove("created_time");
        block.Remove("last_edited_time");
        block.Remove("created_by");
        block.Remove("last_edited_by");
        block.Remove("archived");
        block.Remove("in_trash");
        block.Remove("has_children");
        block.Remove("child_block_ids");

        if (block["type"]?.ToString() == "child_page")
        {
            var children = block["children"]?.Children<JObject>().ToList();
            if (children != null)
            {
                foreach (var child in children)
                {
                    RemoveUnnecessaryProperties(child);
                }
            }
        }
        else
        {
            block.Remove("parent");
            
            var type = block["type"]?.ToString();
            var content = block[type];
            if (content != null && content["children"] != null)
            {
                var children = content["children"]?.Children<JObject>().ToList();
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        RemoveUnnecessaryProperties(child);
                    }
                }
            }
        }
    }

    public static string? ExtractPageId(string html)
    {
        var htmlDoc = html.AsHtmlDocument();
        var metaNode = htmlDoc.DocumentNode.SelectSingleNode($"/html/head/meta[@name='{BlackbirdPageIdAttr}']");
        return metaNode?.Attributes["content"]?.Value;
    }

    public static string ParseBlocks(string pageId, JObject[] blocks)
    {
        var htmlDoc = new HtmlDocument();
        var htmlNode = htmlDoc.CreateElement("html");
        htmlDoc.DocumentNode.AppendChild(htmlNode);

        var headNode = htmlDoc.CreateElement("head");
        htmlNode.AppendChild(headNode);

        var metaNode = htmlDoc.CreateElement("meta");
        metaNode.SetAttributeValue("name", BlackbirdPageIdAttr);
        metaNode.SetAttributeValue("content", pageId);
        headNode.AppendChild(metaNode);

        var bodyNode = htmlDoc.CreateElement("body");
        htmlNode.AppendChild(bodyNode);

        foreach (var block in blocks)
        {
            var type = block["type"]!.ToString();

            if (UnparsableTypes.Contains(type))
                continue;

            var content = block[type]!;
            var id = block["id"]?.ToString();
            var parentPageId = block.GetParentId();

            if (BlockIsUntranslatable(type, content))
                continue;

            RemoveEmptyUrls(block);

            var contentProperties = content.Children();
            var textElements = contentProperties
                .FirstOrDefault(x => TextTypes.Contains((x as JProperty)!.Name))?.Values();

            var blockNode = htmlDoc.CreateElement("div");

            if (textElements is null)
            {
                blockNode.SetAttributeValue(TypeAttr, UntranslatableType);
                blockNode.SetAttributeValue(UntranslatableContentAttr, block.ToString());
                var blockContent = GetBlockContent(block);
                if (!string.IsNullOrEmpty(blockContent))
                {
                    blockNode.InnerHtml = blockContent;
                }
                
                bodyNode.AppendChild(blockNode);
                continue;
            }

            var richText = textElements
                .Select(x => JsonConvert.DeserializeObject<TitleModel>(x.ToString())!)
                .ToArray();

            var contentParams = contentProperties
                .Where(x => !TextTypes.Contains((x as JProperty)!.Name));

            blockNode.SetAttributeValue(TypeAttr, type);
            blockNode.SetAttributeValue(ContentParamsAttr, new JObject(contentParams).ToString());
            blockNode.SetAttributeValue(BlockIdAttr, id);
            blockNode.SetAttributeValue(ParentBlockIdAttr, parentPageId);

            if (block[ChildBlockIds] is not null)
                blockNode.SetAttributeValue(ChildBlockIdsAttr,
                    JsonConvert.SerializeObject(block[ChildBlockIds]));

            foreach (var titleModel in richText)
            {
                var linkUrl = titleModel.Text?.Link?.Url;
                var richTextType = titleModel.Type;

                var blockChildNode = htmlDoc.CreateElement("p");

                if (!string.IsNullOrEmpty(linkUrl))
                    blockChildNode.SetAttributeValue("href", linkUrl);

                if (richTextType == "mention" && titleModel.Mention is not null)
                {
                    var mentionType = titleModel.Mention["type"]?.ToString();
                    blockChildNode.SetAttributeValue("data-mention-id",
                        titleModel.Mention[mentionType]!["id"]?.ToString());
                    blockChildNode.SetAttributeValue("data-mention-type", mentionType);
                }

                if (titleModel.Annotations is not null)
                {
                    var annotations = titleModel.Annotations.AsDictionary()
                        .Select(x => $"{x.Key.ToLower()}={x.Value}")
                        .ToArray();

                    var dataStyle = string.Join(';', annotations);
                    blockChildNode.SetAttributeValue(AnnotationsAttr, dataStyle);
                }

                blockChildNode.InnerHtml = titleModel.Text?.Content ?? titleModel.PlainText;
                blockNode.AppendChild(blockChildNode);
            }

            bodyNode.AppendChild(blockNode);
        }

        return htmlDoc.DocumentNode.OuterHtml;
    }

    private static string? GetBlockContent(JObject jObject)
    {
        var type = jObject["type"]!.ToString();

        if (type == "table_row")
        {
            var cells = jObject["table_row"]!["cells"]!.ToObject<List<List<JObject>>>()?.ToArray();
            var columns = cells.Select(x => x.FirstOrDefault()?["plain_text"]?.ToString()).ToArray();
            
            var html = new StringBuilder();
            for (int i = 0; i < columns.Length; i++)
            {
                html.Append($"<p data-column-number={i}>{columns[i]}</p>");
            }
            
            return html.ToString();
        }
        
        return null;
    }

    public static JObject ParseText(string text)
    {
        var richText = new[]
        {
            new TitleModel()
            {
                Type = "text",
                Text = new()
                {
                    Content = text,
                    Link = null
                }
            }
        };

        var content = new JObject()
        {
            { "rich_text", JArray.Parse(JsonConvert.SerializeObject(richText, JsonConfig.Settings)) },
            { "color", "default" }
        };

        return new()
        {
            { "object", "block" },
            { "type", "paragraph" },
            { "paragraph", content },
        };
    }

    private static JObject ParseNode(HtmlNode node, string type)
    {
        var richText = node.ChildNodes.Select(x =>
        {
            var dataMentionId = x.Attributes["data-mention-id"]?.Value;
            var mentionType = x.Attributes["data-mention-type"]?.Value;
            var richTextType = string.IsNullOrEmpty(dataMentionId) ? "text" : "mention";

            return new TitleModel
            {
                Type = richTextType,
                Annotations = ParseAnnotations(x.Attributes),
                Text = richTextType == "mention"
                    ? null
                    : new()
                    {
                        Content = x.InnerText,
                        Link = x.Attributes["href"]?.Value is null
                            ? null
                            : new()
                            {
                                Url = x.Attributes["href"].Value
                            }
                    },
                Mention = richTextType == "mention"
                    ? new JObject
                    {
                        { "type", mentionType },
                        { mentionType!, new JObject { { "id", dataMentionId } } }
                    }
                    : null,
                PlainText = richTextType == "mention" ? x.InnerText : null!
            };
        }).ToArray();

        var contextParams = new JObject();
        if (node.Attributes[ContentParamsAttr] != null)
            contextParams = JObject.Parse(node.Attributes[ContentParamsAttr]!.DeEntitizeValue);

        var content = new JObject(contextParams)
        {
            { "rich_text", JArray.Parse(JsonConvert.SerializeObject(richText, JsonConfig.Settings)) }
        };

        string? childBlockIds = null;
        if (node.Attributes[ChildBlockIdsAttr] != null)
        {
            childBlockIds = node.Attributes[ChildBlockIdsAttr]!.Value.Replace("&quot;", "\"");
        }

        var jObject = new JObject()
        {
            { "object", "block" },
            { "type", type },
            { type, content },
            { ChildBlockIds, childBlockIds },
            { "id", node.Attributes[BlockIdAttr]?.Value }
        };

        var parentPageId = node.Attributes[ParentBlockIdAttr]?.Value;
        if (parentPageId is not null)
        {
            jObject["parent"] = new JObject
            {
                { "type", "page_id" },
                { "page_id", parentPageId }
            };
        }

        return jObject;
    }

    private static AnnotationsModel? ParseAnnotations(HtmlAttributeCollection attr)
    {
        var data = attr[AnnotationsAttr]?.Value;

        if (data is null)
            return null;

        var annotations = data
            .Split(';')
            .ToDictionary(x => x.Split('=')[0], x => x.Split('=')[1]);

        return JObject.FromObject(annotations).ToObject<AnnotationsModel>();
    }

    private static void RemoveEmptyUrls(JObject block)
    {
        block.Descendants().OfType<JProperty>()
            .Where(x => x is { Name: "url" } && x.Value.ToString() == "#")
            .ToList()
            .ForEach(x => x.Value = null);
    }

    private static bool BlockIsUntranslatable(string type, JToken content)
    {
        if (type == "image" && content["type"].ToString() != "external")
            return true;

        if (type == "callout" && content["icon"]["type"].ToString() == "file")
            (content as JObject).Remove("icon");

        return false;
    }

    private static JObject MapNodeToBlockChild(HtmlNode node)
    {
        var type = node.Attributes[TypeAttr] != null ? node.Attributes[TypeAttr]!.Value : "paragraph";

        return type switch
        {
            UntranslatableType => JObject.Parse(node.Attributes[UntranslatableContentAttr]!.DeEntitizeValue),
            _ => ParseNode(node, type)
        };
    }
}