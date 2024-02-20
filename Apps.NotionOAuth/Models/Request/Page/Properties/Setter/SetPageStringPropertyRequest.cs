using Apps.NotionOAuth.Models.Request.Page.Properties.Getter;

namespace Apps.NotionOAuth.Models.Request.Page.Properties.Setter;

public class SetPageStringPropertyRequest : PageStringPropertyRequest
{
    public string Value { get; set; }
}