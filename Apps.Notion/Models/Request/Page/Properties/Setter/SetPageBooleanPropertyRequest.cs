using Apps.Notion.Models.Request.Page.Properties.Getter;

namespace Apps.Notion.Models.Request.Page.Properties.Setter;

public class SetPageBooleanPropertyRequest : PageStringPropertyRequest
{
    public bool Value { get; set; }
}