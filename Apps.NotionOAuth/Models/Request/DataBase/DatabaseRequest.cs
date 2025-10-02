using Apps.NotionOAuth.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.NotionOAuth.Models.Request.DataBase;

public class DatabaseRequest
{
    [Display("Database ID"), DataSource(typeof(DatabaseDataHandler))]
    public string DatabaseId { get; set; } = string.Empty;
}