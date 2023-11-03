using Apps.Notion.Models.Entities;

namespace Apps.Notion.Models.Response.Block;

public class BlockResponse
{
    public string Id { get; set; }

    public ParentEntity Parent { get; set; }

    public DateTime CreatedTime { get; set; }

    public DateTime? LastEditedTime { get; set; }
    
    public bool HasChildren { get; set; }
    
    public string Type { get; set; }
}