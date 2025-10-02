using Apps.NotionOAuth.Models.Entities;
using Apps.NotionOAuth.Models.Response.DataSource;
using Blackbird.Applications.Sdk.Common;

namespace Apps.NotionOAuth.Models.Response.DataBase;

public class DatabaseResponse
{
    [Display("Database ID")]
    public string Id { get; set; } = string.Empty;

    public DateTime CreatedTime { get; set; }
    
    public DateTime? LastEditedTime { get; set; }
    
    public IEnumerable<TitleModel>? Title { get; set; }

    [Display("Data sources"), DefinitionIgnore]
    public List<DataSourceShortResponse> DataSources { get; set; } = new();
    
    public Dictionary<string, PropertyResponse> Properties { get; set; } = new();
    
    public ParentEntity Parent { get; set; } = new();
    
    public string Url { get; set; } = string.Empty;
    
    public bool Archived { get; set; }
}