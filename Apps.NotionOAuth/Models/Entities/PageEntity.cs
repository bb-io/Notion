using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Models.Response;
using Apps.NotionOAuth.Models.Response.Page;
using Apps.NotionOAuth.Utils;
using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json.Linq;

namespace Apps.NotionOAuth.Models.Entities;

public class PageEntity
{
    [Display("Page ID")] public string Id { get; set; }

    [Display("Created time")] public DateTime? CreatedTime { get; set; }

    [Display("Last edited time")] public DateTime? LastEditedTime { get; set; }

    public ParentEntity Parent { get; set; }

    public string Title { get; set; }

    public bool Archived { get; set; }

    [Display("URL")] public string? Url { get; set; }

    public IEnumerable<PropertyResponse> Properties { get; set; }

    public PageEntity(PageResponse response)
    {
        Id = response.Id;
        CreatedTime = response.CreatedTime;
        LastEditedTime = response.LastEditedTime;
        Parent = response.Parent ?? new ParentEntity { };
        Archived = response.Archived ?? false;
        Url = response.Url;
        Properties = response.Properties?.Select(x => new PropertyResponse(x)) ?? [];
        Title = response.Properties?
            .Where(x => x.Value["type"]?.Value<string>() == DatabasePropertyTypes.Title)
            .Select(x => PagePropertyParser.ToString(x.Value))
            .FirstOrDefault() ?? "Untitled";
    }
}