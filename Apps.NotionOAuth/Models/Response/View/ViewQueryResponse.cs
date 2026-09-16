using Newtonsoft.Json;

namespace Apps.NotionOAuth.Models.Response.View;

public class ViewQueryResponse
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
}