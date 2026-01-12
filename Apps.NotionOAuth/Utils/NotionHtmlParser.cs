using System.Text;
using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Models;
using Apps.NotionOAuth.Models.Request.Page;
using Blackbird.Applications.Sdk.Common.Exceptions;
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

    private static readonly string[] NonTextTranslatableTypes = { "table_row", "child_page", "database" };

    private static readonly string[] TextTypes = { DatabasePropertyTypes.RichText, "text" };

    // main method
    public static JObject[] ParseHtml(string html)
    {
        var htmlDoc = html.AsHtmlDocument();
        var translatableNodes = htmlDoc.DocumentNode.SelectSingleNode("/html/body")
            .ChildNodes
            .Where(x => x.Name != "#text")
            .ToArray();
        var jObjects = translatableNodes.Select(MapNodeToBlockChild).ToList();
        
        var extractedPageId = ExtractAndNormalizePageId(html);
        var blocksById = jObjects.Where(x => x["id"] != null)
            .ToDictionary(x => NormalizeId(x["id"]!.ToString()));

        OrganizeBlocks(jObjects, extractedPageId, blocksById);

        ValidateNotionBlockHierarchy(jObjects);

        jObjects.ForEach(RemoveUnnecessaryProperties);
        return jObjects.ToArray();
    }

    private static string ExtractAndNormalizePageId(string html)
    {
        var extractedPageId = ExtractPageId(html);
        if (string.IsNullOrEmpty(extractedPageId))
            throw new PluginMisconfigurationException("Page ID not found in HTML");
        return NormalizeId(extractedPageId);
    }

    private static void OrganizeBlocks(List<JObject> jObjects, string extractedPageId,
        Dictionary<string, JObject> blocksById)
    {
        for (int i = jObjects.Count - 1; i >= 0; i--)
        {
            var block = jObjects[i];
            var parentId = block.GetParentId();
            if (parentId != null)
            {
                parentId = NormalizeId(parentId);
                if (extractedPageId != parentId)
                {
                    if (blocksById.TryGetValue(parentId, out var parentBlock))
                    {
                        AddBlockToParent(block, parentBlock);
                        jObjects.RemoveAt(i);
                    }
                    else
                    {
                        throw new PluginMisconfigurationException($"Parent block with ID {parentId} not found");
                    }
                }
            }
        }
    }

    private static void AddBlockToParent(JObject block, JObject parentBlock)
    {
        var parentType = parentBlock["type"]?.ToString();
        var parentObject = parentBlock["object"]?.ToString();

        JObject parentContainer;
        if (parentType == "child_page" || (string.IsNullOrEmpty(parentType) && parentObject == "database"))
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

    private static string? GetParentId(this JObject block)
    {
        var parent = block["parent"];
        if (parent != null)
        {
            if (parent["type"]?.ToString() == "page_id")
            {
                return parent["page_id"]?.ToString();
            }

            if (parent["type"]?.ToString() == "block_id")
            {
                return parent["block_id"]?.ToString();
            }

            if (parent["type"]?.ToString() == "database_id")
            {
                return parent["database_id"]?.ToString();
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

        if (block.TryGetValue("object", out var objectType))
        {
            if (objectType.ToString() != "database")
            {
                block.Remove("object");
            }
        }

        block.Remove(DatabasePropertyTypes.CreatedTime);
        block.Remove(DatabasePropertyTypes.LastEditedTime);
        block.Remove(DatabasePropertyTypes.CreatedBy);
        block.Remove(DatabasePropertyTypes.LastEditedBy);
        block.Remove("archived");
        block.Remove("in_trash");
        block.Remove("has_children");
        block.Remove("child_block_ids");

        if (block["type"]?.ToString() == "child_page" || block["object"]?.ToString() == "database")
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
            var content = type == null ? null : block[type];
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

    public static string ParseBlocks(string pageId, JObject[] blocks, GetPageAsHtmlRequest actionRequest)
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
            var type = block["type"]?.ToString();
            var objectType = block["object"]?.ToString();
            if (type != null && UnparsableTypes.Contains(type))
                continue;

            if (string.IsNullOrEmpty(type) && string.IsNullOrEmpty(objectType))
            {
                throw new Exception(
                    "Block and object types are missing. Probably the block is not a valid Notion block. Please send this error to support team.");
            }

            var content = type == null ? null : block[type];
            var id = block["id"]?.ToString();
            var parentPageId = block.GetParentId();

            if (BlockIsUntranslatable(type!, content))
                continue;

            RemoveEmptyUrls(block);

            var contentProperties = content?.Children().ToArray() ?? [];
            var textElements = contentProperties
                .FirstOrDefault(x => TextTypes.Contains((x as JProperty)!.Name))?.Values();

            var blockNode = htmlDoc.CreateElement("div");

            if (textElements is null)
            {
                var typeOrObject = type ?? objectType!;
                var typeAttr = NonTextTranslatableTypes.Contains(typeOrObject) ? typeOrObject : UntranslatableType;
                blockNode.SetAttributeValue(TypeAttr, typeAttr);
                blockNode.SetAttributeValue(UntranslatableContentAttr, block.ToString());
                var blockContent = GetNonTextBlockContent(block, actionRequest);
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

    private static string? GetNonTextBlockContent(JObject jObject, GetPageAsHtmlRequest actionRequest)
    {
        var type = jObject["type"]?.ToString();
        var objectType = jObject["object"]?.ToString();

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

        if (type == "child_page" && objectType == "block")
        {
            var title = jObject["child_page"]![DatabasePropertyTypes.Title]?.ToString();
            return string.IsNullOrEmpty(title)
                ? null
                : $"<p data-property-id=\"title\" data-property-name=\"title\" data-property-type=\"{DatabasePropertyTypes.Title}\">{title}</p>";
        }

        if (type == null && objectType == "database")
        {
            var title = jObject["title"]?.ToObject<List<JObject>>()?.ToArray();
            if (title is not null)
            {
                var html = new StringBuilder();
                var titleElement = title.FirstOrDefault();
                if (titleElement is not null)
                {
                    var text = titleElement["text"]!["content"]?.ToString();
                    if (text is not null)
                    {
                        html.Append($"<p>{text}</p>");
                    }
                }

                return html.ToString();
            }
        }

        if (type == "child_page" && objectType == "page")
        {
            var properties = jObject["properties"] as JObject;
            if (properties != null)
            {
                var html = new StringBuilder();

                foreach (var property in properties)
                {
                    var propertyName = property.Key;
                    var propertyValue = property.Value as JObject;
                    var propertyId = propertyValue?["id"]!.ToString();
                    var propertyType = propertyValue?["type"]?.ToString();

                    var valueString = string.Empty;
                    switch (propertyType)
                    {
                        case DatabasePropertyTypes.Title:
                        case DatabasePropertyTypes.RichText:
                            var includePageProperties = actionRequest.IncludePageProperties ?? false;
                            if (!includePageProperties && propertyType != DatabasePropertyTypes.Title)
                            {
                                break;
                            }

                            var richTexts = propertyValue?[propertyType] as JArray;
                            if (richTexts != null)
                            {
                                foreach (var richText in richTexts)
                                {
                                    var textContent = richText["plain_text"]?.ToString();
                                    if (textContent != null)
                                    {
                                        valueString += textContent;
                                    }
                                }
                            }

                            break;
                    }

                    if (!string.IsNullOrEmpty(valueString))
                    {
                        html.Append(
                            $"<p data-property-id=\"{propertyId}\" data-property-name=\"{propertyName}\" data-property-type=\"{propertyType}\">{valueString}</p>");
                    }
                }

                return html.ToString();
            }
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
            { DatabasePropertyTypes.RichText, JArray.Parse(JsonConvert.SerializeObject(richText, JsonConfig.Settings)) },
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

        if (string.Equals(type, "callout", StringComparison.OrdinalIgnoreCase) && (!contextParams.TryGetValue("icon", out var iconToken) || iconToken.Type == JTokenType.Null))
        {
            contextParams.Remove("icon");
        }

        RemoveNullsDeep(contextParams);

        var content = new JObject(contextParams)
        {
            { DatabasePropertyTypes.RichText, JArray.Parse(JsonConvert.SerializeObject(richText, JsonConfig.Settings)) }
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

    public static void RemoveNullsDeep(JToken token)
    {
        if (token is JObject obj)
        {
            foreach (var prop in obj.Properties().ToList())
            {
                if (prop.Value == null || prop.Value.Type == JTokenType.Null)
                {
                    prop.Remove();
                }
                else
                {
                    RemoveNullsDeep(prop.Value);
                }
            }
        }
        else if (token is JArray arr)
        {
            foreach (var item in arr.ToList())
                RemoveNullsDeep(item);
        }
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
            .ForEach(x => x.Value = JValue.CreateNull());
    }

    private static bool BlockIsUntranslatable(string? type, JToken? content)
    {
        if (string.IsNullOrEmpty(type) || content is null)
            return false;

        if (content.Type != JTokenType.Object)
            return false;

        var obj = (JObject)content;

        if (type == "image")
        {
            var imageType = (obj["type"] as JValue)?.ToString();
            if (!string.Equals(imageType, "external", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        if (type == "callout")
        {
            var iconObj = obj["icon"] as JObject;
            var iconType = iconObj?["type"]?.ToString();
            if (iconType == "file")
                obj.Remove("icon");
        }

        return false;
    }

    private static JObject MapNodeToBlockChild(HtmlNode node)
    {
        var type = node.Attributes[TypeAttr] != null ? node.Attributes[TypeAttr]!.Value : "paragraph";

        return type switch
        {
            UntranslatableType => JObject.Parse(node.Attributes[UntranslatableContentAttr]!.DeEntitizeValue),
            "table_row" => ParseRowTableNode(node),
            "child_page" => ParseChildPage(node),
            "database" => ParseDatabaseNode(node),
            _ => ParseNode(node, type)
        };
    }

    private static JObject ParseRowTableNode(HtmlNode node)
    {
        var cells = node.ChildNodes
       .Where(x => x.Name == "p")
       .Select(x => x.InnerText)
       .ToArray();

        var tableRow = JObject.Parse(node.Attributes[UntranslatableContentAttr]!.DeEntitizeValue);
        var tableCells = tableRow["table_row"]!["cells"]!.ToObject<List<List<JObject>>>()!;

        for (int i = 0; i < cells.Length; i++)
        {
            if (i >= tableCells.Count)
                continue;

            var cellItems = tableCells[i];
            if (cellItems == null || cellItems.Count == 0)
                continue;

            var textCell = cellItems.FirstOrDefault(x => x?["type"]?.ToString() == "text");
            if (textCell == null)
            {
                continue;
            }

            if (textCell["text"] is not JObject textObj)
            {
                textObj = new JObject { ["content"] = "", ["link"] = null };
                textCell["text"] = textObj;
            }

            textObj["content"] = cells[i];
            textCell["plain_text"] = cells[i];
        }

        tableRow["table_row"]!["cells"] = JArray.FromObject(tableCells);
        return tableRow;
    }

    private static JObject ParseChildPage(HtmlNode node)
    {
        var childPage = JObject.Parse(node.Attributes[UntranslatableContentAttr]!.DeEntitizeValue);
        var paragraphs = node.ChildNodes.Where(x => x.Name == "p").ToList();
        var childPageProperties = childPage["properties"] as JObject;

        if (childPageProperties == null)
        {
            var title = paragraphs.FirstOrDefault()?.InnerText ?? "Untilted";
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
        }
        else
        {
            foreach (var paragraph in paragraphs)
            {
                var dataPropertyId = paragraph.Attributes["data-property-id"]?.Value;
                var dataPropertyName = paragraph.Attributes["data-property-name"]?.Value;
                var dataPropertyType = paragraph.Attributes["data-property-type"]?.Value;

                if (dataPropertyId != null && dataPropertyName != null && dataPropertyType != null)
                {
                    var propertyToUpdate = childPageProperties[dataPropertyName] as JObject;
                    if (propertyToUpdate == null)
                        continue;

                    var type = propertyToUpdate["type"]!.ToString();
                    var typeElement = propertyToUpdate[type]?.ToObject<List<JObject>>()?.FirstOrDefault()
                                      ?? throw new Exception(
                                          $"Couldn't find any editable element for json: {JsonConvert.SerializeObject(propertyToUpdate, Formatting.Indented)}");

                    var typeOfTextElement = typeElement["type"]!.ToString();
                    typeElement[typeOfTextElement]!["content"] = paragraph.InnerText;
                    propertyToUpdate[type] =
                        JArray.Parse(
                            JsonConvert.SerializeObject(new List<JObject> { typeElement }, JsonConfig.Settings));
                    childPageProperties[dataPropertyName] = propertyToUpdate;
                }
            }
        }

        return childPage;
    }

    private static JObject ParseDatabaseNode(HtmlNode node)
    {
        var database = JObject.Parse(node.Attributes[UntranslatableContentAttr]!.DeEntitizeValue);
        var title = node.ChildNodes.FirstOrDefault(x => x.Name == "p")?.InnerText;
        if (title is not null)
        {
            var titles = database["title"]!.ToObject<List<JObject>>();
            var firstTitle = titles?.FirstOrDefault();

            if (firstTitle is not null)
            {
                firstTitle["text"]!["content"] = title;
                database["title"] = JArray.FromObject(titles!);
            }
        }

        if (database.TryGetValue("developer_survey", out _))
        {
            database.Remove("developer_survey");
        }

        if (database.TryGetValue("request_id", out _))
        {
            database.Remove("request_id");
        }

        return database;
    }

    private static void ValidateNotionBlockHierarchy(List<JObject> blocks)
    {
        foreach (var block in blocks)
        {
            ValidateBlockParents(block);
        }
    }

    private static void ValidateBlockParents(JObject block)
    {
        if (IsDatabaseOrChildPage(block))
        {
            var parent = block["parent"] as JObject;
            var parentType = parent?["type"]?.ToString();
            if (parentType != "page_id" && parentType != "database_id")
            {
                string blockName = GetBlockName(block);
                throw new PluginApplicationException(
                    $"Pages and databases cannot be nested inside other blocks. Page or database ('{blockName}') has parent type '{parentType}'. Please move it to the root level.");
            }
        }

        if (block.TryGetValue("children", out JToken childrenToken) && childrenToken is JArray children)
        {
            foreach (var child in children.OfType<JObject>())
            {
                ValidateBlockParents(child);
            }
        }

        var type = block["type"]?.ToString();
        if (!string.IsNullOrEmpty(type) && block[type] is JObject content &&
            content.TryGetValue("children", out JToken nestedChildren) && nestedChildren is JArray nestedArray)
        {
            foreach (var child in nestedArray.OfType<JObject>())
            {
                ValidateBlockParents(child);
            }
        }
    }
    
    private static string GetBlockName(JObject block)
    {
        try
        {
            if (string.Equals(block["type"]?.ToString(), "child_page", StringComparison.OrdinalIgnoreCase))
            {
                var title = block["child_page"]?["title"]?.ToString();
                if (!string.IsNullOrWhiteSpace(title))
                    return title;
            }

            if (block["object"]?.ToString() == "database" && block["title"] is JArray titleArray && titleArray.Count > 0)
            {
                var title = titleArray.First?["plain_text"]?.ToString();
                if (string.IsNullOrWhiteSpace(title))
                    title = titleArray.First?["text"]?["content"]?.ToString();
                if (!string.IsNullOrWhiteSpace(title))
                    return title;
            }

            return block["id"]?.ToString() ?? "unknown";
        }
        catch (Exception)
        {
            return block["id"]?.ToString() ?? "unknown";
        }
    }

    private static bool IsDatabaseOrChildPage(JObject block)
    {
        var type = block["type"]?.ToString();
        if (string.Equals(type, "child_page", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        if (block["object"]?.ToString() == "database")
        {
            return true;
        }
        
        return false;
    }
}