using Newtonsoft.Json;

namespace Apps.NotionOAuth.Models;

public class TitlePropertyModel
{
    [JsonProperty("title")]
    public List<TitleProperty> Title { get; set; } = new();
}

public class TitleProperty
{
    [JsonProperty("text")]
    public TextContentModel Text { get; set; } = new();
}

public class TextContentModel
{
    [JsonProperty("content")]
    public string Content { get; set; } = string.Empty;
}