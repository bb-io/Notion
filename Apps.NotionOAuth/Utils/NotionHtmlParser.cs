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
        
        var allBlocks = jObjects;
        var processedIds = new HashSet<string>();
        var childBlockIds = new HashSet<string>();

        var blocksById = allBlocks.Where(x => x["id"] != null)
            .ToDictionary(x => x["id"]!.ToString());

        foreach (var block in allBlocks)
        {
            if (block[ChildBlockIds] != null)
            {
                List<string> childIds;
                if (block[ChildBlockIds]!.Type == JTokenType.String)
                {
                    childIds = JsonConvert.DeserializeObject<List<string>>(block[ChildBlockIds]!.ToString()) ?? new();
                }
                else
                {
                    childIds = block[ChildBlockIds]?.ToObject<List<string>>() ?? new();
                }

                foreach (var childId in childIds)
                {
                    childBlockIds.Add(childId);
                }
            }
        }

        var rootBlocks = new List<JObject>();

        foreach (var block in allBlocks)
        {
            var id = block["id"]?.ToString();
            if (id == null || !childBlockIds.Contains(id))
            {
                ProcessBlock(block, blocksById, processedIds);
                rootBlocks.Add(block);
            }
        }

        rootBlocks = rootBlocks.Where(x => !childBlockIds.Contains(x["id"]?.ToString())).ToList();
        var childPages = rootBlocks.Where(x => x.GetStringValue("type") == "child_page").ToList();
        var innerChildBlocksIds = new List<string>();

        foreach (var childPage in childPages)
        {
            var title = childPage["child_page"]?["title"]?.ToString() ?? "Untitled";
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
            childPage["properties"] = titlePropertyJson;
            
            var children = rootBlocks.Where(y => y.GetParentPageId() == childPage.GetStringValue("id")).ToList();
            children.ForEach(y => y.Remove("id"));
            childPage["children"] = JArray.FromObject(children);
            
            innerChildBlocksIds.AddRange(children.Select(y => y.GetStringValue("id"))!);

            if (childPage.TryGetValue("child_page", out _))
            {
                childPage.Remove("child_page");
            }
            
            if (childPage.TryGetValue("has_children", out _))
            {
                childPage.Remove("has_children");
            }
            
            if (childPage.TryGetValue("object", out _))
            {
                childPage.Remove("object");
            }
            
            if (childPage.TryGetValue("object", out _))
            {
                childPage.Remove("object");
            }
        }
        
        rootBlocks = rootBlocks.Where(x => !innerChildBlocksIds.Contains(x.GetStringValue("id")!)).ToList();
        rootBlocks.ForEach(x => x.Remove("id"));
        return rootBlocks.ToArray();
    }

    private static void ProcessBlock(JObject block, Dictionary<string, JObject> blocksById, HashSet<string> processedIds)
    {
        var id = block["id"]?.ToString();

        if (id != null)
        {
            if (processedIds.Contains(id))
            {
                return;
            }
            
            processedIds.Add(id);
        }

        if (block[ChildBlockIds] != null)
        {
            List<string> childIds;
            if (block[ChildBlockIds]!.Type == JTokenType.String)
            {
                childIds = JsonConvert.DeserializeObject<List<string>>(block[ChildBlockIds]!.ToString()) ?? new();
            }
            else
            {
                childIds = block[ChildBlockIds]?.ToObject<List<string>>() ?? new();
            }

            if (childIds.Count > 0)
            {
                var type = block["type"]!.ToString();
                var children = new List<JObject>();

                foreach (var childId in childIds)
                {
                    if (blocksById.TryGetValue(childId, out var childBlock))
                    {
                        ProcessBlock(childBlock, blocksById, processedIds);
                        children.Add(childBlock);
                    }
                }

                if (children.Count > 0)
                {
                    block[type]!["children"] = JArray.FromObject(children);
                }
            }

            block.Remove(ChildBlockIds);
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
            var parentPageId = block.GetParentPageId();

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
                    blockChildNode.SetAttributeValue("data-mention-id", titleModel.Mention[mentionType]!["id"]?.ToString());
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
                Text = richTextType == "mention" ? null : new()
                {
                    Content = x.InnerText,
                    Link = x.Attributes["href"]?.Value is null
                        ? null
                        : new()
                        {
                            Url = x.Attributes["href"].Value
                        }
                },
                Mention = richTextType == "mention" ? new JObject
                {
                    { "type", mentionType },
                    { mentionType!, new JObject { { "id", dataMentionId } } }
                } : null,
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