using Apps.NotionOAuth.Models.Response.Comment;
using Blackbird.Applications.Sdk.Common;

namespace Apps.NotionOAuth.Models.Entities;

public class CommentEntity
{
    [Display("Comment ID")]
    public string Id { get; set; }

    [Display("Page ID")]
    public string? PageId { get; set; }

    [Display("Discussion ID")]
    public string? DiscussionId { get; set; }

    [Display("Created time")]
    public DateTime CreatedTime { get; set; }

    [Display("Last edited time")]
    public DateTime? LastEditedTime { get; set; }

    public string Text { get; set; }

    public CommentEntity(CommentResponse response)
    {
        Id = response.Id;
        PageId = response.Parent?.PageId;
        DiscussionId = response.DiscussionId;
        CreatedTime = response.CreatedTime;
        LastEditedTime = response.LastEditedTime;
        Text = response.RichText.First().PlainText;
    }
}