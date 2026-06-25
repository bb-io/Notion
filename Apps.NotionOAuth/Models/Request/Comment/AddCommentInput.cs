using Apps.NotionOAuth.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using System.Collections.Generic;

namespace Apps.NotionOAuth.Models.Request.Comment;

public class AddCommentInput
{
    [Display("Page ID")]
    [DataSource(typeof(PageDataHandler))]
    public string? PageId { get; set; }
    
    [Display("Discussion ID")]
    public string? DiscussionId { get; set; }
    
    public string Text { get; set; } = string.Empty;

    [Display("Mentioned user IDs", Description = "Users will be appended to the end of the comment as mentions")]
    [DataSource(typeof(UserDataHandler))]
    public IEnumerable<string>? MentionedUserIds { get; set; }
}
