using Apps.Notion.Models.Request.Page.Properties.Getter;

namespace Apps.Notion.Models.Request.Page.Properties.Setter;

public class SetPageNumberPropertyRequest : PageStringPropertyRequest
{
    public decimal Value { get; set; }
}