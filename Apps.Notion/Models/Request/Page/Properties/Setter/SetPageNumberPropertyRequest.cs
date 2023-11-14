using Apps.Notion.Models.Request.Page.Properties.Getter;

namespace Apps.Notion.Models.Request.Page.Properties.Setter;

public class SetPageNumberPropertyRequest : PageNumberPropertyRequest
{
    public decimal Value { get; set; }
}