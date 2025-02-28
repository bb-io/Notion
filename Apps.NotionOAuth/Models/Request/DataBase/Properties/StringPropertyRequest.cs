using Apps.NotionOAuth.DataSourceHandlers.PageProperties.Getters;
using Apps.NotionOAuth.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common;

namespace Apps.NotionOAuth.Models.Request.DataBase.Properties;

public class StringPropertyRequest
{
    [Display("Database ID"), DataSource(typeof(DatabaseDataHandler))]
    public string DatabaseId { get; set; } = string.Empty;

    [Display("Property ID"), DataSource(typeof(StringPagePropertiesDataHandler))]
    public string PropertyId { get; set; } = string.Empty;
}