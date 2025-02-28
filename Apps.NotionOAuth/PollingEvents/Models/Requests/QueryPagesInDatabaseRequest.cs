using Apps.NotionOAuth.DataSourceHandlers;
using Apps.NotionOAuth.DataSourceHandlers.DatabaseProperties;
using Apps.NotionOAuth.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.NotionOAuth.PollingEvents.Models.Requests;

public class QueryPagesInDatabaseRequest
{
    [Display("Database ID"), DataSource(typeof(DatabaseDataHandler))]
    public string DatabaseId { get; set; } = string.Empty;
    
    [Display("Status property name"), DataSource(typeof(AllDatabasePropertyDataHandler))]
    public string StatusPropertyName { get; set; } = string.Empty;
    
    [Display("Status property type", Description = "Type of the property"), StaticDataSource(typeof(FilterPropertyTypeDataHandler))]
    public string StatusPropertyType { get; set; } = string.Empty;
    
    [Display("Status property value")]
    public string StatusPropertyValue { get; set; } = string.Empty;
}