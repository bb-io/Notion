using Newtonsoft.Json;

namespace Apps.NotionOAuth.CallbackEvents.Models.Dto;
public class ButtonRequest
{
    [JsonProperty("data")]
    public Data Data { get; set; } = new();

    [JsonProperty("parent")]
    public Parent Parent { get; set; } = new();
}

public class Data
{
    [JsonProperty("id")]
    public string PageId { get; set; } = string.Empty;
}

public class Parent
{
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("database_id")]
    public string DatabaseId { get; set; } = string.Empty;

    [JsonProperty("page_id")]
    public string PageId { get; set; } = string.Empty;

    [JsonProperty("block_id")]
    public string BlockId { get; set; } = string.Empty;

    public string GetParentId()
    {
        return Type switch
        {
            "database_id" => DatabaseId,
            "page_id" => PageId,
            "block_id" => BlockId,
            _ => string.Empty
        };
    }
}