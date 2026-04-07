using Apps.NotionOAuth.Extensions;
using Apps.NotionOAuth.Models.Entities;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.NotionOAuth.Models.Request.Comment;

public class AddCommentRequest
{
    public ParentEntity Parent { get; set; }

    public string? DiscussionId { get; set; }

    public List<TitleModel>? RichText { get; set; }

    public AddCommentRequest(AddCommentInput input)
    {
        DiscussionId = input.DiscussionId;
        Parent = new() { PageId = input.PageId };
        RichText = [];

        var textChunks = input.Text.ChunkString(2000);
        if (textChunks.Count() > 100)
            throw new PluginMisconfigurationException("Comment exceeds Notion's limit of 200000 characters");

        foreach (var chunk in textChunks)
            RichText?.Add(new() { Text = new () { Content = chunk } });
    }
}