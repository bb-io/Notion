using Apps.NotionOAuth.Models.Entities;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.NotionOAuth.Models.Response.Page;

public record ListPagesResponse(List<PageEntity> Items) : IMultiDownloadableContentOutput<PageEntity>
{
    public List<PageEntity> Items { get; set; } = Items;
}