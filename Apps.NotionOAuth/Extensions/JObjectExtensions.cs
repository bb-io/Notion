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
            DatabasePropertyTypes.Url => obj["url"]!.ToString(),
            DatabasePropertyTypes.Title => HandleTitleObject(obj["title"]!),
            DatabasePropertyTypes.Email => obj["email"]!.ToString(),
            DatabasePropertyTypes.PhoneNumber => obj["phone_number"]!.ToString(),
            DatabasePropertyTypes.Status => obj["status"]!["name"]!.ToString(),
            DatabasePropertyTypes.CreatedBy => obj["created_by"]!["id"]!.ToString(),
            DatabasePropertyTypes.LastEditedBy => obj["last_edited_by"]!["id"]!.ToString(),
            DatabasePropertyTypes.Select => obj["select"]!["name"]!.ToString(),
            DatabasePropertyTypes.RichText => obj["rich_text"]!.ToObject<TitleModel>()!.PlainText,
            _ => throw new ArgumentException("Given property is not of type string")
        };
    }

    private static string? HandleTitleObject(JToken title)
    {
        var titleString = title.ToString();

        if(titleString.StartsWith("["))
        {
            return title.ToObject<TitleModel[]>()!.FirstOrDefault()?.PlainText;
        }

        return title.ToObject<TitleModel>()!.PlainText;
    }
}