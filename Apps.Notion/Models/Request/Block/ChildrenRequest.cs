using Newtonsoft.Json.Linq;

namespace Apps.Notion.Models.Request.Block;

public class ChildrenRequest
{
    public JObject[] Children { get; set; }
}