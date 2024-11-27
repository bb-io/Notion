using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.NotionOAuth.Models;

public class TitleModel
{
    public string Type { get; set; }
  
    public TextModel? Text { get; set; }

    public JObject? Mention { get; set; } = null;
  
    public AnnotationsModel? Annotations { get; set; }
    
    [JsonProperty("plain_text")]
    public string PlainText { get; set; }
}