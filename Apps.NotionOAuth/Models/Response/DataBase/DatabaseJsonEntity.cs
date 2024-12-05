using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.NotionOAuth.Models.Response.DataBase;

public class DatabaseJsonEntity
{    
    [JsonProperty("object")]
    public string Object { get; set; } = string.Empty;
        
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("parent")]
    public JObject Parent { get; set; } = new();

    [JsonProperty("properties")]
    public JObject Properties { get; set; } = new();

    [JsonProperty("cover")]
    public JObject? Cover { get; set; }

    [JsonProperty("icon")]
    public JObject? Icon { get; set; } 
}