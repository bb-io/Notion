using Apps.NotionOAuth.Constants;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.NotionOAuth.Utils;

public static class PagePropertyParser
{
    public static string ToString(JToken? payload)
    {
        if (payload == null || payload.Type != JTokenType.Object)
            throw new PluginApplicationException($"Payload is null or not an object. Payload received: {JsonConvert.SerializeObject(payload)}");

        var type = payload["type"]?.Value<string>()
            ?? throw new PluginApplicationException($"Property payload does not contain a property type. Payload received: {JsonConvert.SerializeObject(payload)}");

        return type switch
        {
            DatabasePropertyTypes.Checkbox => GetStringSafe(payload, "checkbox"),
            DatabasePropertyTypes.CreatedBy => GetStringSafe(payload, "created_by.name"),
            DatabasePropertyTypes.CreatedTime => GetStringSafe(payload, "created_time"),
            DatabasePropertyTypes.Date => GetStringSafe(payload, "date.start") + (!string.IsNullOrEmpty(GetStringSafe(payload, "date.end")) ? " - " + GetStringSafe(payload, "end") : string.Empty),
            DatabasePropertyTypes.Email => GetStringSafe(payload, "email"),
            DatabasePropertyTypes.Files => GetCommaSeparatedStringFromArray(payload, "files", "name"),
            DatabasePropertyTypes.Formula => GetStringFromFormula(payload["formula"]),
            DatabasePropertyTypes.LastEditedBy => GetStringSafe(payload, "last_edited_by.name"),
            DatabasePropertyTypes.LastEditedTime => GetStringSafe(payload, "last_edited_time"),
            DatabasePropertyTypes.MultiSelect => GetCommaSeparatedStringFromArray(payload, "multi_select", "name"),
            DatabasePropertyTypes.Number => GetStringSafe(payload, "number"),
            DatabasePropertyTypes.People => GetCommaSeparatedStringFromArray(payload, "people", "name"),
            DatabasePropertyTypes.PhoneNumber => GetStringSafe(payload, "phone_number"),
            DatabasePropertyTypes.Relation => GetCommaSeparatedStringFromArray(payload, "relation", "id"),
            DatabasePropertyTypes.Rollup => GetStringFromRollup(payload["rollup"]),
            DatabasePropertyTypes.RichText => GetPlainTextFromRichText(payload, "rich_text"),
            DatabasePropertyTypes.Select => GetStringSafe(payload, "select.name"),
            DatabasePropertyTypes.Status => GetStringSafe(payload, "status.name"),
            DatabasePropertyTypes.Title => GetPlainTextFromRichText(payload, "title"),
            DatabasePropertyTypes.Url => GetStringSafe(payload, "url"),
            DatabasePropertyTypes.UniqueId => $"{GetStringSafe(payload, "unique_id.prefix")}-{GetStringSafe(payload, "unique_id.number")}",
            DatabasePropertyTypes.Verification => GetStringSafe(payload, "verification.state"),
            _ => string.Empty
        };
    }

    private static string GetStringSafe(JToken? payload, string jsonPath)
    {
        var token = payload?.SelectToken(jsonPath);
        return token?.Value<string>() ?? string.Empty;
    }

    private static string GetPlainTextFromRichText(JToken? payload, string jsonPath)
    {
        var richText = payload?.SelectToken(jsonPath) as JArray;

        if (richText == null || richText.Type != JTokenType.Array || !richText.Any())
            return string.Empty;

        return string.Join(
            string.Empty,
            richText.Select(item => item["plain_text"]?.Value<string>() ?? string.Empty));
    }

    private static string GetCommaSeparatedStringFromArray(JToken? payload, string jsonPathOfArray, string jsonPathToPick)
    {
        var array = payload?.SelectToken(jsonPathOfArray) as JArray;
        if (array == null || array.Type != JTokenType.Array || !array.Any())
            return string.Empty;

        return string.Join(
            ", ",
            array.Select(item => item.SelectToken(jsonPathToPick)?.Value<string>() ?? string.Empty));
    }

    private static string GetStringFromFormula(JToken? payload)
    {
        if (payload == null || payload.Type != JTokenType.Object)
            return string.Empty;

        var type = payload["type"]?.Value<string>();

        return type switch
        {
            "string" => payload["string"]?.Value<string>() ?? string.Empty,
            "number" => payload["number"]?.Value<string>() ?? string.Empty,
            "date" => payload["date"]?.Type == JTokenType.Object ? payload["date"]["start"]?.Value<string>() ?? string.Empty : string.Empty,
            "boolean" => payload["boolean"]?.Value<string>() ?? string.Empty,
            _ => string.Empty
        };
    }

    private static string GetStringFromRollup(JToken? payload)
    {
        if (payload == null)
            return string.Empty;

        // Handle primitive values (JValue) that can occur in recursive calls
        if (payload.Type != JTokenType.Object)
        {
            return payload.Value<string>() ?? string.Empty;
        }

        var type = payload["type"]?.Value<string>();

        // Special case of rollup array without access to data
        // when Blackbird receives an array of empty relations
        if (type == "array")
        {
            var rollupArray = payload["array"] as JArray;
            if (rollupArray?.Type == JTokenType.Array || rollupArray?.Any() == false)
            {
                var value = string.Join(string.Empty, rollupArray?.Select(i => ToString(i)) ?? []);
                if (string.IsNullOrEmpty(value))
                    return "This page contains rollup data. However, due to current Notion API limitations, we are unable to display this information here. Use the related page IDs to fetch their data via the 'Get page' action.";
            }
        }

        return type switch
        {
            "array" => string.Join(", ", payload["array"]?.Select(i => ToString(i)) ?? []),
            "number" => payload["number"]?.Value<string>() ?? string.Empty,
            "date" => payload["date"]?.Type == JTokenType.Object ? payload["date"]["start"]?.Value<string>() ?? string.Empty : string.Empty,
            "incomplete" => "Incomplete",
            "unsupported" => "Unsupported",
            _ => string.Empty
        };
    }
}
