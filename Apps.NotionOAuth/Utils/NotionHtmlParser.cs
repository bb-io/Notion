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
        { "child_page", "child_database", "unsupported", "file", "link_preview" };

    private static readonly string[] TextTypes = { "rich_text", "text" };

    public static JObject[] ParseHtml(byte[] file)
    {
        var html = Encoding.UTF8.GetString(file).AsHtmlDocument();
        var translatableNodes = html.DocumentNode.SelectSingleNode("/html/body")
            .ChildNodes
            .Where(x => x.Name != "#text")
            .ToArray();

        return translatableNodes.Select(MapNodeToBlockChild).ToArray();
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
        var type = node.Attributes[TypeAttr]!.Value;

        return type switch
        {
            UntranslatableType => JObject.Parse(node.Attributes[UntranslatableContentAttr]!.DeEntitizeValue),
            _ => ParseText(node, type)
        };
    }

    private static JObject ParseText(HtmlNode node, string type)
    {
        var richText = node.ChildNodes.Select(x => new TitleModel()
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

        var contextParams = JObject.Parse(node.Attributes[ContentParamsAttr]!.DeEntitizeValue);
        var content = new JObject(contextParams)
        {
            { "rich_text", JArray.Parse(JsonConvert.SerializeObject(richText, JsonConfig.Settings)) }
        };

        return new()
        {
            { "object", "block" },
            { "type", type },
            { type, content },
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