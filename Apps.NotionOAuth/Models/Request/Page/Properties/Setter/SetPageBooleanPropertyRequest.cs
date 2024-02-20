using Apps.NotionOAuth.Models.Request.Page.Properties.Getter;

namespace Apps.NotionOAuth.Models.Request.Page.Properties.Setter;

public class SetPageBooleanPropertyRequest : PageBooleanPropertyRequest
{
    public bool Value { get; set; }
}