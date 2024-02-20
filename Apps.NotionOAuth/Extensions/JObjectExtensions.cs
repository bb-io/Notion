using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.NotionOAuth.Extensions;

public static class JObjectExtensions
{
    public static JObject ToJObject(this object obj)
        => JObject.FromObject(obj, JsonSerializer.Create(JsonConfig.Settings));

    public static string? GetStringValue(this JObject obj)
    {
        return obj["type"]!.ToString() switch
        {
            "url" => obj["url"]!.ToString(),
            "title" => obj["title"]!.ToObject<TitleModel>()!.PlainText,
            "email" => obj["email"]!.ToString(),
            "phone_number" => obj["phone_number"]!.ToString(),
            "status" => obj["status"]!["name"]!.ToString(),
            "created_by" => obj["created_by"]!["id"]!.ToString(),
            "last_edited_by" => obj["last_edited_by"]!["id"]!.ToString(),
            "select" => obj["select"]!["name"]!.ToString(),
            "rich_text" => obj["rich_text"]!.ToObject<TitleModel>()!.PlainText,
            _ => throw new ArgumentException("Given property is not of type string")
        };
    }
}