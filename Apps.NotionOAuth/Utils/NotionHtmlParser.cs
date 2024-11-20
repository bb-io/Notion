using System.Text;
using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Models;
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

    private static readonly string[] UnparsableTypes =
        { "child_page", "child_database", "unsupported", "link_preview" };

    private static readonly string[] TextTypes = { "rich_text", "text" };

    public static JObject[] ParseHtml(byte[] file)
    {
        var html = Encoding.UTF8.GetString(file).AsHtmlDocument();
        var translatableNodes = html.DocumentNode.SelectSingleNode("/html/body")
            .ChildNodes
            .Where(x => x.Name != "#text")
            .ToArray();

        var jObjects = translatableNodes.Select(MapNodeToBlockChild).ToList();
        var json = JsonConvert.SerializeObject(jObjects, Formatting.Indented); // this JSON I generated and gave you 
        
        var allBlocks = jObjects;
        var processedIds = new HashSet<string>();
        var childBlockIds = new HashSet<string>();

        // Build a dictionary of blocks by their ID for quick lookup
        var blocksById = allBlocks.Where(x => x["id"] != null)
            .ToDictionary(x => x["id"]!.ToString());

        // First, collect all child block IDs
        foreach (var block in allBlocks)
        {
            if (block["child_block_ids"] != null)
            {
                List<string> childIds;
                if (block["child_block_ids"]!.Type == JTokenType.String)
                {
                    childIds = JsonConvert.DeserializeObject<List<string>>(block["child_block_ids"]!.ToString()) ?? new();
                }
                else
                {
                    childIds = block["child_block_ids"]?.ToObject<List<string>>() ?? new();
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
                // Avoid processing the same block multiple times (prevent cycles)
                return;
            }
            processedIds.Add(id);
        }

        if (block["child_block_ids"] != null)
        {
            List<string> childIds;
            if (block["child_block_ids"]!.Type == JTokenType.String)
            {
                childIds = JsonConvert.DeserializeObject<List<string>>(block["child_block_ids"]!.ToString()) ?? new();
            }
            else
            {
                childIds = block["child_block_ids"]?.ToObject<List<string>>() ?? new();
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

            block.Remove("child_block_ids"); // Remove the child_block_ids after processing
        }
    }
    
    public static string ParseBlocks(JObject[] blocks)
    {
        var htmlDoc = new HtmlDocument();
        var htmlNode = htmlDoc.CreateElement("html");
        htmlDoc.DocumentNode.AppendChild(htmlNode);
        htmlNode.AppendChild(htmlDoc.CreateElement("head"));

        var bodyNode = htmlDoc.CreateElement("body");
        htmlNode.AppendChild(bodyNode);

        foreach (var block in blocks)
        {
            var type = block["type"]!.ToString();

            if (UnparsableTypes.Contains(type))
                continue;

            var content = block[type]!;
            var id = block["id"]?.ToString();

            if (BlockIsUntranslatable(type, content))
                continue;

            RemoveEmptyUrls(block);

            var contentProperties = content.Children();
            var textElements = contentProperties
                .FirstOrDefault(x => TextTypes.Contains((x as JProperty)!.Name))?.Values();

            var blockNode = htmlDoc.CreateElement("div");

            if (type == "file" || type == "audio")
            {
                var updatedBlock = new JObject(block);

                var originalBlock = block[type]!;
                var url = originalBlock["file"]?["url"] ?? originalBlock["external"]?["url"]
                    ?? throw new Exception(
                        "We couldn't find the file url. Please send this error to the support team along with page ID.");

                updatedBlock[type] = new JObject
                {
                    { "caption", originalBlock["caption"] },
                    { "type", "external" },
                    {
                        "external", new JObject
                        {
                            { "url", url }
                        }
                    },
                    { "name", originalBlock["name"] }
                };

                blockNode.SetAttributeValue(TypeAttr, "file");
                blockNode.SetAttributeValue(UntranslatableContentAttr, updatedBlock.ToString());
                bodyNode.AppendChild(blockNode);

                continue;
            }
            else if (textElements is null)
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
            blockNode.SetAttributeValue("data-block-id", id);

            if (block["child_block_ids"] is not null)
                blockNode.SetAttributeValue("data-child-block-ids",
                    JsonConvert.SerializeObject(block["child_block_ids"]));

            foreach (var titleModel in richText)
            {
                var linkUrl = titleModel.Text?.Link?.Url;

                var blockChildNode = htmlDoc.CreateElement("p");

                if (!string.IsNullOrEmpty(linkUrl))
                    blockChildNode.SetAttributeValue("href", linkUrl);

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

    private static void RemoveEmptyUrls(JObject block)
    {
        block.Descendants().OfType<JProperty>()
            .Where(x => x is { Name: "url" } && x.Value.ToString() == "#")
            .ToList()
            .ForEach(x => x.Value = null);
    }

    private static bool BlockIsUntranslatable(string type, JToken content)
    {
        if (type == "column_list" && !content.Children().Any())
            return true;

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
            "file" => JObject.Parse(node.Attributes[UntranslatableContentAttr]!.DeEntitizeValue),
            _ => ParseNode(node, type)
        };
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
        var richText = node.ChildNodes.Select(x => new TitleModel
        {
            Type = "text",
            Annotations = ParseAnnotations(x.Attributes),
            Text = new()
            {
                Content = x.InnerText,
                Link = x.Attributes["href"]?.Value is null
                    ? null
                    : new()
                    {
                        Url = x.Attributes["href"].Value
                    }
            }
        }).ToArray();

        var contextParams = new JObject();
        if (node.Attributes[ContentParamsAttr] != null)
            contextParams = JObject.Parse(node.Attributes[ContentParamsAttr]!.DeEntitizeValue);

        var content = new JObject(contextParams)
        {
            { "rich_text", JArray.Parse(JsonConvert.SerializeObject(richText, JsonConfig.Settings)) }
        };

        string? childBlockIds = null;
        if (node.Attributes["data-child-block-ids"] != null)
        {
            childBlockIds = node.Attributes["data-child-block-ids"]!.Value.Replace("&quot;", "\"");
        }

        return new()
        {
            { "object", "block" },
            { "type", type },
            { type, content },
            { "child_block_ids", childBlockIds },
            { "id", node.Attributes["data-block-id"]?.Value }
        };
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
}