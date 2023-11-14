using Apps.Notion.DataSourceHandlers;
using Apps.Notion.DataSourceHandlers.PageProperties;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Notion.Models.Request.Page.Properties.Getter;

public class PageNumberPropertyRequest
{
    [Display("Page")]
    [DataSource(typeof(PageDataHandler))]
    public string PageId { get; set; }
    
    [Display("Property")]
    [DataSource(typeof(NumberPagePropertiesDataHandler))]
    public string PropertyId { get; set; }
}