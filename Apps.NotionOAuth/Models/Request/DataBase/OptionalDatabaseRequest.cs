using Apps.NotionOAuth.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.NotionOAuth.Models.Request.DataBase;

public class OptionalDatabaseRequest
{
    [Display("Database ID", Description = "At least one of database ID or data source ID is required")]
    [DataSource(typeof(DatabaseDataHandler))]
    public string? DatabaseId { get; set; }
}
