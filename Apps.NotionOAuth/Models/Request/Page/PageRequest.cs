using Apps.NotionOAuth.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.SDK.Extensions.FileManagement.Models.FileDataSourceItems;

namespace Apps.NotionOAuth.Models.Request.Page;

public class PageRequest
{
    [Display("Page ID")]
    [DataSource(typeof(PageDataHandler))]
    public string PageId { get; set; }
}