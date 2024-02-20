using Apps.NotionOAuth.Models.Response.Block;
using Blackbird.Applications.Sdk.Common;

namespace Apps.NotionOAuth.Models.Entities;

public class BlockEntity
{
    [Display("Block ID")] public string Id { get; set; }

    [Display("Parent page ID")] public string? PageId { get; set; }

    [Display("Created time")] public DateTime CreatedTime { get; set; }

    [Display("Last edited time")] public DateTime? LastEditedTime { get; set; }

    [Display("Has children")] public bool HasChildren { get; set; }

    public string Type { get; set; }

    public BlockEntity(BlockResponse response)
    {
        Id = response.Id;
        PageId = response.Parent.PageId;
        CreatedTime = response.CreatedTime;
        LastEditedTime = response.LastEditedTime;
        HasChildren = response.HasChildren;
        Type = response.Type;
    }
}