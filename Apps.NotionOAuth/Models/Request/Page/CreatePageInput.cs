using Apps.NotionOAuth.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.NotionOAuth.Models.Request.Page;

public class CreatePageInput
{
    public string Title { get; set; }

    [Display("Parent page ID")]
    [DataSource(typeof(PageDataHandler))]
    public string? PageId { get; set; }

    [Display("Parent database ID")]
    [DataSource(typeof(DatabaseDataHandler))]
    public string? DatabaseId { get; set; }
    
    [Display("Parent datasource ID"), DataSource(typeof(DataSourceDataHandler))]
    public string? DataSourceId { get; set; }
}