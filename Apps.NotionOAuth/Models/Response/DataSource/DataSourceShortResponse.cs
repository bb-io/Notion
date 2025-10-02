using Blackbird.Applications.Sdk.Common;

namespace Apps.NotionOAuth.Models.Response.DataSource;

public class DataSourceShortResponse
{
    [Display("Data source ID")]
    public string Id { get; set; } = string.Empty;
    
    [Display("Data source name")]
    public string Name { get; set; } = string.Empty;
}