using Apps.Notion.Models.Response;
using Apps.Notion.Models.Response.DataBase;
using Blackbird.Applications.Sdk.Common;

namespace Apps.Notion.Models.Entities;

public class DatabaseEntity
{
    [Display("Database ID")]
    public string Id { get; set; }

    [Display("Created time")]
    public DateTime CreatedTime { get; set; }

    [Display("Last edited time")]
    public DateTime LastEditedTime { get; set; }

    public string? Title { get; set; }

    public IEnumerable<PropertyResponse> Properties { get; set; }

    [Display("URL")]
    public string Url { get; set; }

    public bool Archived { get; set; }
    
    public DatabaseEntity(DatabaseResponse response)
    {
        Id = response.Id;
        CreatedTime = response.CreatedTime;
        LastEditedTime = response.LastEditedTime;
        Title = response.Title.FirstOrDefault()?.PlainText;
        Properties = response.Properties.Values;
        Url = response.Url;
        Archived = response.Archived;
    }
}