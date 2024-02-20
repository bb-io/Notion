using Apps.NotionOAuth.DataSourceHandlers;
using Apps.NotionOAuth.DataSourceHandlers.PageProperties.Getters;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.NotionOAuth.Models.Request.Page.Properties.Getter;

public class PageFilesPropertyRequest
{
    [Display("Database")]
    [DataSource(typeof(DatabaseDataHandler))]
    public string DatabaseId { get; set; }  
    
    [Display("Page")]
    [DataSource(typeof(PageDataHandler))]
    public string PageId { get; set; }
    
    [Display("Property")]
    [DataSource(typeof(FilesPagePropertiesDataHandler))]
    public string PropertyId { get; set; }
}