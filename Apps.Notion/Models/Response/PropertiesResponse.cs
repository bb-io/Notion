using Newtonsoft.Json.Linq;

namespace Apps.Notion.Models.Response;

public class PropertiesResponse
{
    public Dictionary<string, JObject> Properties { get; set; }
}