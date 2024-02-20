using Apps.NotionOAuth.Models.Request.Page.Properties.Getter;

namespace Apps.NotionOAuth.Models.Request.Page.Properties.Setter;

public class SetPageNumberPropertyRequest : PageNumberPropertyRequest
{
    public decimal Value { get; set; }
}