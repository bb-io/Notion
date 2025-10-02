using Apps.NotionOAuth.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.NotionOAuth.Models.Request.Page;

public class PageOptionalRequest
{
    [Display("Page ID"), DataSource(typeof(PageDataHandler))]
    public string? PageId { get; set; }
}