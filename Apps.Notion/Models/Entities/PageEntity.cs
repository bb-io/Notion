using Apps.Notion.Models.Response;
using Apps.Notion.Models.Response.Page;
using Blackbird.Applications.Sdk.Common;

namespace Apps.Notion.Models.Entities;

public class PageEntity
{
    [Display("Page ID")] public string Id { get; set; }

    [Display("Created time")] public DateTime CreatedTime { get; set; }

    [Display("Last edited time")] public DateTime? LastEditedTime { get; set; }

    public ParentEntity Parent { get; set; }

    public string Title { get; set; }

    public bool Archived { get; set; }

    [Display("URL")] public string Url { get; set; }

    public IEnumerable<PropertyResponse> Properties { get; set; }

    public PageEntity(PageResponse response)
    {
        Id = response.Id;
        CreatedTime = response.CreatedTime;
        LastEditedTime = response.LastEditedTime;
        Parent = response.Parent;
        Archived = response.Archived;
        Url = response.Url;
        Properties = response.Properties.Select(x => new PropertyResponse(x));
        Title =
            response.Properties.FirstOrDefault(x => x.Value["title"]?.FirstOrDefault()?["plain_text"] is not null)
                .Value?["title"]?[0]?["plain_text"]?.ToString() ?? "Untitled";
    }
}