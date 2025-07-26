using Apps.NotionOAuth.Utils;
using Newtonsoft.Json.Linq;

namespace Apps.NotionOAuth.Models.Response;

public class PropertyResponse
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    
    public string Type { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public PropertyResponse() { }

    public PropertyResponse(KeyValuePair<string, JObject> pair)
    {
        Id = pair.Value["id"]!.ToString();
        Name = pair.Key;
        Type = pair.Value["type"]!.ToString();
        Value = PagePropertyParser.ToString(pair.Value);
    }
}