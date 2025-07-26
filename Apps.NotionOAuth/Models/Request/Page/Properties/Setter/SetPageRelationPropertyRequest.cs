using Apps.NotionOAuth.Models.Request.Page.Properties.Getter;
using Blackbird.Applications.Sdk.Common;

namespace Apps.NotionOAuth.Models.Request.Page.Properties.Setter;

public class SetPageRelationPropertyRequest : PageFilesPropertyRequest
{
    [Display("Page IDs")]
    public IEnumerable<string> RelatedPageIds { get; set; } = [];
}