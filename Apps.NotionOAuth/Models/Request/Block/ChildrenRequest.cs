using Newtonsoft.Json.Linq;

namespace Apps.NotionOAuth.Models.Request.Block;

public class ChildrenRequest
{
    public JObject[] Children { get; set; }
}