using Apps.Notion.Models.Request.Page.Properties.Getter;

namespace Apps.Notion.Models.Request.Page.Properties.Setter;

public class SetPageMultipleValuePropertyRequest : PageMultipleStringPropertyRequest
{
    public IEnumerable<string> Values { get; set; }
}