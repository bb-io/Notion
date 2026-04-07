using Apps.NotionOAuth.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.NotionOAuth.Models.Request.DataSource;

public class DataSourceRequest
{
    [Display("Data source ID"), DataSource(typeof(DataSourceDataHandler))]
    public string DataSourceId { get; set; } = string.Empty;
}