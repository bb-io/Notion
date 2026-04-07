using Apps.NotionOAuth.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.NotionOAuth.Models.Request.View;

public class ViewRequest
{
    [Display("View ID"), DataSource(typeof(ViewDataHandler))]
    public string ViewId { get; set; } = string.Empty;
}
