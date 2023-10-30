using Blackbird.Applications.Sdk.Common;

namespace Apps.Notion.Models.Entities;

public class ParentEntity
{
    public string Type { get; set; }
    
    [Display("Page ID")]
    public string? PageId { get; set; }
    
    [Display("Database ID")]
    public string? DatabaseId { get; set; }
}