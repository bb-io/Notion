using Newtonsoft.Json;

namespace Apps.NotionOAuth.Models;

public class TitleModel
{
    public string Type { get; set; }
  
    public TextModel Text { get; set; }
  
    public AnnotationsModel? Annotations { get; set; }
    
    [JsonProperty("plain_text")]
    public string PlainText { get; set; }
}