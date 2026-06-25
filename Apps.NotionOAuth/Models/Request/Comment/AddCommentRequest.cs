using Apps.NotionOAuth.Extensions;
using Apps.NotionOAuth.Models.Entities;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Apps.NotionOAuth.Models.Request.Comment;

public class AddCommentRequest
{
    public ParentEntity? Parent { get; set; }

    public string? DiscussionId { get; set; }

    public List<TitleModel>? RichText { get; set; }

    public AddCommentRequest(AddCommentInput input)
    {
        DiscussionId = input.DiscussionId;
        Parent = string.IsNullOrWhiteSpace(input.PageId)
            ? null
            : new() { PageId = input.PageId };
        RichText = [];

        var text = input.Text ?? string.Empty;
        var textChunks = text.ChunkString(2000).ToList();
        if (textChunks.Count > 100)
            throw new PluginMisconfigurationException("Comment exceeds Notion's limit of 200000 characters");

        foreach (var chunk in textChunks)
            RichText?.Add(new() { Text = new () { Content = chunk } });

        var mentionedUserIds = input.MentionedUserIds?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToArray() ?? [];

        if (!textChunks.Any() && mentionedUserIds.Length == 0)
            throw new PluginMisconfigurationException("Comment must contain text or at least one mentioned user");

        if (mentionedUserIds.Length > 0)
        {
            if (!string.IsNullOrEmpty(text) && !char.IsWhiteSpace(text[^1]))
            {
                RichText?.Add(new()
                {
                    Text = new()
                    {
                        Content = " "
                    }
                });
            }

            for (var i = 0; i < mentionedUserIds.Length; i++)
            {
                RichText?.Add(CreateUserMention(mentionedUserIds[i]));

                if (i < mentionedUserIds.Length - 1)
                {
                    RichText?.Add(new()
                    {
                        Text = new()
                        {
                            Content = " "
                        }
                    });
                }
            }
        }

        if (RichText?.Count > 100)
            throw new PluginMisconfigurationException("Comment exceeds Notion's limit of 100 rich text elements");
    }

    private static TitleModel CreateUserMention(string userId)
    {
        return new()
        {
            Type = "mention",
            Mention = new JObject
            {
                { "type", "user" },
                {
                    "user", new JObject
                    {
                        { "id", userId },
                        { "object", "user" }
                    }
                }
            }
        };
    }
}
