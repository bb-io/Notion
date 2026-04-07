using Apps.NotionOAuth.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.NotionOAuth.Models.Request.DataSource;

public class OptionalDataSourceRequest
{
    [Display("Data source ID", Description = "At least one of database ID or data source ID is required")]
    [DataSource(typeof(DataSourceDataHandler))]
    public string? DataSourceId { get; set; }
}
