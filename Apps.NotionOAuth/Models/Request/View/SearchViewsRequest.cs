using Blackbird.Applications.Sdk.Common;

namespace Apps.NotionOAuth.Models.Request.View;

public class SearchViewsRequest
{
    [Display("View name contains")]
    public string? ViewNameContains { get; set; }
}
