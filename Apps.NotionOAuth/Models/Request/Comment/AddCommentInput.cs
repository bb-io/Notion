using Apps.NotionOAuth.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.NotionOAuth.Models.Request.Comment;

public class AddCommentInput
{
    [Display("Page ID")]
    [DataSource(typeof(PageDataHandler))]
    public string? PageId { get; set; }
    
    [Display("Discussion ID")]
    public string? DiscussionId { get; set; }
    
    public string Text { get; set; }
}