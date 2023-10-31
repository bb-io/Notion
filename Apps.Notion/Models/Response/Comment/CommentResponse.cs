using Apps.Notion.Models.Entities;

namespace Apps.Notion.Models.Response.Comment;

public class CommentResponse
{
    public string Id { get; set; }
    
    public ParentEntity? Parent { get; set; }

    public string? DiscussionId { get; set; }
    
    public DateTime CreatedTime { get; set; }
    
    public DateTime? LastEditedTime { get; set; }
    
    public TitleModel[] RichText { get; set; }
}