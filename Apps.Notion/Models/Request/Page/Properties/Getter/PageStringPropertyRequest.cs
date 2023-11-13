using Apps.Notion.DataSourceHandlers;
using Apps.Notion.DataSourceHandlers.PageProperties;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Notion.Models.Request.Page.Properties.Getter;

public class PageStringPropertyRequest
{
    [Display("Page")]
    [DataSource(typeof(PageDataHandler))]
    public string PageId { get; set; }
    
    [Display("Property")]
    [DataSource(typeof(StringPagePropertiesDataHandler))]
    public string PropertyId { get; set; }
}