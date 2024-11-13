using Apps.NotionOAuth.Models.Request.Page.Properties.Getter;
using Blackbird.Applications.Sdk.Common;

namespace Apps.NotionOAuth.Models.Request.Page.Properties.Setter;

public class SetPageMultipleValuePropertyRequest : PageMultipleStringPropertyRequest
{
    public IEnumerable<string> Values { get; set; }

    [Display("Add on update", Description = "Instead of overwriting the values with new values, the new values will be appended to the existing values.")]
    public bool? AddOnUpdate { get; set; }

    [Display("Set property as blank")]
    public bool? RemoveValues { get; set; }
}