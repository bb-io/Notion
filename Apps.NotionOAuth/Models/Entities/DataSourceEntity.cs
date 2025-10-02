using Apps.NotionOAuth.Models.Response;
using Apps.NotionOAuth.Models.Response.DataSource;
using Blackbird.Applications.Sdk.Common;

namespace Apps.NotionOAuth.Models.Entities;

public class DataSourceEntity(DataSourceResponse response)
{
    [Display("Data source ID")]
    public string Id { get; set; } = response.Id;

    [Display("Created time")]
    public DateTime CreatedTime { get; set; } = response.CreatedTime;

    [Display("Last edited time")]
    public DateTime? LastEditedTime { get; set; } = response.LastEditedTime;

    public string Title { get; set; } = response.Title?.FirstOrDefault()?.PlainText ?? "Untitled";

    public IEnumerable<PropertyResponse> Properties { get; set; } = response.Properties.Values;

    [Display("URL")]
    public string Url { get; set; } = response.Url;

    public bool Archived { get; set; } = response.Archived;

    public ParentEntity Parent { get; set; } = response.Parent;
}