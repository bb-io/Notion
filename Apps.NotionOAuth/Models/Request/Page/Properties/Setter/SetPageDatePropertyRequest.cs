using Apps.NotionOAuth.DataSourceHandlers;
using Apps.NotionOAuth.DataSourceHandlers.PageProperties.Setters;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.NotionOAuth.Models.Request.Page.Properties.Setter;

public class SetPageDatePropertyRequest
{
    [Display("Database")]
    [DataSource(typeof(DatabaseDataHandler))]
    public string DatabaseId { get; set; }  
    
    [Display("Page")]
    [DataSource(typeof(PageDataHandler))]
    public string PageId { get; set; }
    
    [Display("Property")]
    [DataSource(typeof(SetDatePagePropertyDataHandler))]
    public string PropertyId { get; set; }
    
    public DateTime Date { get; set; }
    
    [Display("End date")]
    public DateTime? EndDate { get; set; }
    
    [Display("Include time")]
    public bool? IncludeTime { get; set; }
}