using Apps.NotionOAuth.Extensions;
using Apps.NotionOAuth.Models.Entities;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text.RegularExpressions;

namespace Apps.NotionOAuth.Models.Request.Comment;

public class AddCommentRequest
{
    private static readonly Regex MentionUserRegex = new(
        @"<mention-user\s+url=""([^""]+)""\s*>(.*?)</mention-user>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

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
        AddTextWithInlineMentions(text);

        var mentionedUserIds = input.MentionedUserIds?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToArray() ?? [];

        if (RichText?.Count == 0 && mentionedUserIds.Length == 0)
            throw new PluginMisconfigurationException("Comment must contain text or at least one mentioned user");

        if (mentionedUserIds.Length > 0)
        {
            if (ShouldAddSeparatorBeforeAppendedMention())
            {
                AddTextChunk(" ");
            }

            for (var i = 0; i < mentionedUserIds.Length; i++)
            {
                RichText?.Add(CreateUserMention(mentionedUserIds[i]));

                if (i < mentionedUserIds.Length - 1)
                    AddTextChunk(" ");
            }
        }

        if (RichText?.Count > 100)
            throw new PluginMisconfigurationException("Comment exceeds Notion's limit of 100 rich text elements");
    }

    private void AddTextWithInlineMentions(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var currentIndex = 0;
        var matches = MentionUserRegex.Matches(text);

        foreach (Match match in matches)
        {
            if (!match.Success)
                continue;

            AddTextChunk(text[currentIndex..match.Index]);
            RichText?.Add(CreateUserMention(match.Groups[1].Value));

            currentIndex = match.Index + match.Length;
        }

        AddTextChunk(text[currentIndex..]);
    }

    private void AddTextChunk(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var textChunks = text.ChunkString(2000).ToList();
        if (RichText!.Count + textChunks.Count > 100)
            throw new PluginMisconfigurationException("Comment exceeds Notion's limit of 200000 characters");

        foreach (var chunk in textChunks)
            RichText.Add(new() { Text = new() { Content = chunk } });
    }

    private bool ShouldAddSeparatorBeforeAppendedMention()
    {
        if (RichText == null || RichText.Count == 0)
            return false;

        var lastElement = RichText.Last();
        if (lastElement.Type == "mention")
            return true;

        var lastText = lastElement.Text?.Content;
        return !string.IsNullOrEmpty(lastText) && !char.IsWhiteSpace(lastText[^1]);
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
