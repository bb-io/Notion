using Newtonsoft.Json.Linq;

namespace Apps.NotionOAuth.Models.Response;

public class PropertyResponse
{
    public string Id { get; set; }
    
    public string Name { get; set; }
    
    public string Type { get; set; }

    public PropertyResponse()
    {
        
    }

    public PropertyResponse(KeyValuePair<string, JObject> pair)
    {
        Name = pair.Key;
        Type = pair.Value["type"]!.ToString();
        Id = pair.Value["id"]!.ToString();
    }
}