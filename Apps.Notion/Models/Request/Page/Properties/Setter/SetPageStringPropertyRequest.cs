using Apps.Notion.Models.Request.Page.Properties.Getter;

namespace Apps.Notion.Models.Request.Page.Properties.Setter;

public class SetPageStringPropertyRequest : PageStringPropertyRequest
{
    public string Value { get; set; }
}