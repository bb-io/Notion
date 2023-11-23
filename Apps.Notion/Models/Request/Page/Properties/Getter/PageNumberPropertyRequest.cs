using Apps.Notion.DataSourceHandlers;
using Apps.Notion.DataSourceHandlers.PageProperties.Getters;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Notion.Models.Request.Page.Properties.Getter;

public class PageNumberPropertyRequest
{
    [Display("Database")]
    [DataSource(typeof(DatabaseDataHandler))]
    public string DatabaseId { get; set; }  
    
    [Display("Page")]
    [DataSource(typeof(PageDataHandler))]
    public string PageId { get; set; }
    
    [Display("Property")]
    [DataSource(typeof(NumberPagePropertiesDataHandler))]
    public string PropertyId { get; set; }
}