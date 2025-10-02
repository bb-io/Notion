using Apps.NotionOAuth.Models.Response.Page;
using Newtonsoft.Json.Linq;

namespace Apps.NotionOAuth.Utils;

public static class PageResponseExtensions
{
    public static bool FilterCheckboxProperty(this PageResponse pageResponse, string inputCheckboxProperty)
    {
        var propertyData = inputCheckboxProperty.Split(';');

        var propertyId = propertyData[0];
        var propertyValue = propertyData[1];

        return pageResponse.Properties.Any(x =>
            x.Value["id"]!.ToString() == propertyId && x.Value["checkbox"]?.ToString() == propertyValue);
    }
    
    public static bool PagePropertyHasValue(this PageResponse page, string propertyId)
    {
        KeyValuePair<string, JObject>? propertyPair =
            page.Properties.FirstOrDefault(x => x.Value["id"].ToString() == propertyId);

        var property = propertyPair?.Value ?? throw new("No property found with the provided ID");
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
        var propertyData = inputSelectProperty.Split(';');

        var propertyId = propertyData[0];
        var propertyValue = propertyData[1];

        return pageResponse.Properties.Any(x =>
            x.Value["id"]!.ToString() == propertyId && x.Value.SelectToken("select.name")?.ToString() == propertyValue);
    }
}