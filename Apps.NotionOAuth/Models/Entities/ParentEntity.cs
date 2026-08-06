using Blackbird.Applications.Sdk.Common;

namespace Apps.NotionOAuth.Models.Entities;

public class ParentEntity
{
    public string? Type { get; set; }
    
    [Display("Parent page ID")]
    public string? PageId { get; set; }
    
    [Display("Parent database ID")]
    public string? DatabaseId { get; set; }
    
    [Display("Parent data source ID")]
    public string? DataSourceId { get; set; }
}
