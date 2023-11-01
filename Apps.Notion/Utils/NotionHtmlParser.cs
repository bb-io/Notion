using System.Text;
using Apps.Notion.Constants;
using Apps.Notion.Models;
using Blackbird.Applications.Sdk.Utils.Extensions.System;
using Blackbird.Applications.Sdk.Utils.Html.Extensions;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.Notion.Utils;

public static class NotionHtmlParser
{
    private const string Signature = "bb990011";

    private static Dictionary<string, string> Types => new()
    {
        { "h1", "heading_1" },
        { "h2", "heading_2" },
        { "h3", "heading_3" },
        { "p", "paragraph" },
    };

    public static JObject[] ParseHtml(byte[] file)
    {
        var html = Encoding.UTF8.GetString(file).AsHtmlDocument();
        var translatableNodes = html.DocumentNode.SelectSingleNode("/html/body")
            .Descendants()
            .Where(x => x.Name != "#text" && !string.IsNullOrWhiteSpace(x.InnerText) && x.ChildNodes.Count == 1)
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
            var typeData = block[type];
            var richText = JsonConvert.DeserializeObject<TitleModel[]>(typeData["rich_text"].ToString());

            foreach (var titleModel in richText)
            {
                var linkUrl = titleModel.Text?.Link?.Url;

                var blockNode = htmlDoc.CreateElement(GetBlockTagName(type));
                if (!string.IsNullOrEmpty(linkUrl))
                    blockNode.SetAttributeValue("href", linkUrl);

                if (titleModel.Annotations is not null)
                {
                    var annotations = titleModel.Annotations.AsDictionary()
                        .Select(x => $"{x.Key.ToLower()}={x.Value}")
                        .ToArray();

                    var dataStyle = $"{Signature}{string.Join(';', annotations)}";
                    blockNode.SetAttributeValue("data-style", dataStyle);
                }

                blockNode.InnerHtml = titleModel.Text?.Content;
                bodyNode.AppendChild(blockNode);
            }
        }

        return htmlDoc.DocumentNode.OuterHtml;
    }

    private static JObject MapNodeToBlockChild(HtmlNode node)
    {
        var richText = new TitleModel[]
        {
            new()
            {
                Type = "text",
                Annotations = ParseAnnotations(node.Attributes),
                Text = new()
                {
                    Content = node.InnerText,
                    Link = node.Attributes["href"]?.Value is null
                        ? null
                        : new()
                        {
                            Url = node.Attributes["href"].Value
                        }
                }
            }
        };

        var type = GetBlockType(node.Name);
        return new()
        {
            { "object", "block" },
            { "type", type },
            {
                type, new JObject()
                {
                    { "rich_text", JArray.Parse(JsonConvert.SerializeObject(richText, JsonConfig.Settings)) }
                }
            },
        };
    }

    private static AnnotationsModel? ParseAnnotations(HtmlAttributeCollection attr)
    {
        var data = attr["data-style"]?.Value;

        if (data is null || !data.StartsWith(Signature))
            return null;

        var annotations = data
            .Replace(Signature, string.Empty)
            .Split(';')
            .ToDictionary(x => x.Split('=')[0], x => x.Split('=')[1]);

        return JObject.FromObject(annotations).ToObject<AnnotationsModel>();
    }

    private static string GetBlockType(string tagName)
        => Types.TryGetValue(tagName, out var type) ? type : "paragraph";

    private static string GetBlockTagName(string blockType)
        => Types.ContainsValue(blockType) ? Types.First(x => x.Value == blockType).Key : "div";
}