using Newtonsoft.Json;
using Blackbird.Applications.Sdk.Common;

namespace Apps.NotionOAuth.Models.Response.View;

public class ViewResponse
{
    [Display("View ID"), JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [Display("View name"), JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [Display("View type"), JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [Display("Created at"), JsonProperty("created_time")]
    public DateTime CreatedAt { get; set; }

    [Display("Edited at"), JsonProperty("last_edited_time")]
    public DateTime LastEditedAt { get; set; }

    [Display("View URL"), JsonProperty("url")]
    public string Url { get; set; } = string.Empty;

    [Display("Data source ID"), JsonProperty("data_source_id")]
    public string? DataSourceId { get; set; }
}
