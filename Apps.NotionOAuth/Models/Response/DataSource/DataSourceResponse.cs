using Apps.NotionOAuth.Models.Entities;
using Blackbird.Applications.Sdk.Common;

namespace Apps.NotionOAuth.Models.Response.DataSource;

public class DataSourceResponse
{
    [Display("Data source ID")]
    public string Id { get; set; } = string.Empty;

    public DateTime CreatedTime { get; set; }
    
    public DateTime? LastEditedTime { get; set; }
    
    public IEnumerable<TitleModel>? Title { get; set; }
    
    public Dictionary<string, PropertyResponse> Properties { get; set; } = new();
    
    public ParentEntity Parent { get; set; } = new();
    
    public string Url { get; set; } = string.Empty;
    
    public bool Archived { get; set; }
}