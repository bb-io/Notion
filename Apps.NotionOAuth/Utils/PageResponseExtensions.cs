using Apps.NotionOAuth.Models.Response.Page;
using Newtonsoft.Json.Linq;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.NotionOAuth.Utils;

public static class PageResponseExtensions
{
    public static bool FilterCheckboxProperty(this PageResponse pageResponse, string inputCheckboxProperty)
    {
        var propertyData = ParsePropertyFilter(inputCheckboxProperty);

        var propertyId = propertyData[0];
        var propertyValue = propertyData[1];

        return pageResponse.Properties.Any(x =>
            x.Value["id"]!.ToString() == propertyId && x.Value["checkbox"]?.ToString() == propertyValue);
    }
    
    public static bool PagePropertyHasValue(this PageResponse page, string propertyId)
    {
        KeyValuePair<string, JObject>? propertyPair =
            page.Properties.FirstOrDefault(x => x.Value["id"].ToString() == propertyId);

        var property = propertyPair?.Value
            ?? throw new PluginMisconfigurationException("No property found with the provided ID");
        var propertyType = property["type"].ToString();

        return propertyType switch
        {
            "formula" => property[propertyType][property[propertyType]["type"].ToString()].HasValues ||
                         (property[propertyType][property[propertyType]["type"].ToString()] as JValue)?.Value != null,
            "rollup" => property[propertyType][property[propertyType]["type"].ToString()].HasValues ||
                        (property[propertyType][property[propertyType]["type"].ToString()] as JValue)?.Value != null,
            _ => property[propertyType].HasValues || (property[propertyType] as JValue)?.Value != null,
        };
    }
    
    public static bool FilterSelectProperty(this PageResponse pageResponse, string inputSelectProperty)
    {
        var propertyData = ParsePropertyFilter(inputSelectProperty);

        var propertyId = propertyData[0];
        var propertyValue = propertyData[1];

        return pageResponse.Properties.Any(x =>
            x.Value["id"]!.ToString() == propertyId && x.Value.SelectToken("select.name")?.ToString() == propertyValue);
    }

    private static string[] ParsePropertyFilter(string input)
    {
        var propertyData = input.Split(';', 2);
        if (propertyData.Length != 2)
        {
            throw new PluginMisconfigurationException(
                "Property filter must contain a property ID and value separated by ';'.");
        }

        return propertyData;
    }
}