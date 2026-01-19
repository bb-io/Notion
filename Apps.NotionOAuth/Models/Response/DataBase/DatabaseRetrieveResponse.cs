using Newtonsoft.Json;

namespace Apps.NotionOAuth.Models.Response.DataBase
{
    public class DatabaseRetrieveResponse
    {
        [JsonProperty("properties")]
        public Dictionary<string, DatabasePropertySchema> Properties { get; set; } = new();
    }
    public class DatabasePropertySchema
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;
    }
}
