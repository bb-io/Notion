using Apps.NotionOAuth.Models.Entities;

namespace Apps.NotionOAuth.Models.Request.Comment;

public class AddCommentRequest
{
    public ParentEntity Parent { get; set; }

    public string? DiscussionId { get; set; }

    public TitleModel[]? RichText { get; set; }

    public AddCommentRequest(AddCommentInput input)
    {
        DiscussionId = input.DiscussionId;
        Parent = new()
        {
            PageId = input.PageId
        };
        RichText = new TitleModel[]
        {
            new()
            {
                Text = new()
                {
                    Content = input.Text
                }
            }
        };
    }
}