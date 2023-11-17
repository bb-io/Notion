using Apps.Notion.DataSourceHandlers;
using Apps.Notion.DataSourceHandlers.DatabaseProperties;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Notion.Models.Request.DataBase;

public class SearchPagesInDatabaseRequest : ListRequest
{
    [Display("Database")]
    [DataSource(typeof(DatabaseDataHandler))]
    public string DatabaseId { get; set; }
    
    [Display("Checkbox property")]
    [DataSource(typeof(CheckboxDatabasePropertyDataHandler))]
    public string? CheckboxProperty { get; set; }
    
    [Display("Select property")]
    [DataSource(typeof(SelectDatabasePropertyDataHandler))]
    public string? SelectProperty { get; set; }
}