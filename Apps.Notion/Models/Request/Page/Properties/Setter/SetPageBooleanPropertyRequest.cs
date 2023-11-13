using Apps.Notion.Models.Request.Page.Properties.Getter;

namespace Apps.Notion.Models.Request.Page.Properties.Setter;

public class SetPageBooleanPropertyRequest : PageBooleanPropertyRequest
{
    public bool Value { get; set; }
}