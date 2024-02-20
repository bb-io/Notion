using Apps.NotionOAuth.Models.Request.Page.Properties.Getter;

namespace Apps.NotionOAuth.Models.Request.Page.Properties.Setter;

public class SetPageMultipleValuePropertyRequest : PageMultipleStringPropertyRequest
{
    public IEnumerable<string> Values { get; set; }
}