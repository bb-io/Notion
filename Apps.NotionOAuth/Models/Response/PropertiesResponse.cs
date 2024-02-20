using Newtonsoft.Json.Linq;

namespace Apps.NotionOAuth.Models.Response;

public class PropertiesResponse
{
    public Dictionary<string, JObject> Properties { get; set; }
}