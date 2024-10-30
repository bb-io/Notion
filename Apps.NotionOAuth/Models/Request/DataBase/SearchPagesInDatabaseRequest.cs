using Apps.NotionOAuth.DataSourceHandlers;
using Apps.NotionOAuth.DataSourceHandlers.DatabaseProperties;
using Apps.NotionOAuth.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.NotionOAuth.Models.Request.DataBase;

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

    [Display("Properties with value", Description = "DEPRECATED. This property will be used only to in-memory filtering"), DataSource(typeof(AllDatabasePropertyDataHandler))]
    public IEnumerable<string>? PropertiesShouldHaveValue { get; set; }

    [Display("Properties without value", Description = "DEPRECATED. This property will be used only to in-memory filtering"), DataSource(typeof(AllDatabasePropertyDataHandler))]
    public IEnumerable<string>? PropertiesWithoutValues { get; set; }

    [Display("Filter property"), DataSource(typeof(AllDatabasePropertyDataHandler))]
    public string? FilterProperty { get; set; }
    
    [Display("Filter value must be empty")]
    public bool? FilterValueIsEmpty { get; set; }
    
    [Display("Filter value")]
    public string? FilterValue { get; set; }
    
    [Display("Filter property type"), StaticDataSource(typeof(FilterPropertyTypeDataHandler))]
    public string? FilterPropertyType { get; set; }
}