using Newtonsoft.Json.Linq;

namespace Apps.NotionOAuth.Models.Response;

public class PropertyResponse
{
    public string Id { get; set; }
    
    public string Name { get; set; }
    
    public string Type { get; set; }

    public string Value { get; set; }

    public PropertyResponse()
    { }

    public PropertyResponse(KeyValuePair<string, JObject> pair)
    {
        Id = pair.Value["id"]!.ToString();
        Name = pair.Key;
        Type = pair.Value["type"]!.ToString();
        if (pair.Value[Type] is JObject typeObject && typeObject["name"] != null)
        {
            Value = typeObject["name"]?.ToString() ?? string.Empty;
        }
    }
}