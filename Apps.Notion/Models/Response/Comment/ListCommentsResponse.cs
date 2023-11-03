using Apps.Notion.Models.Entities;

namespace Apps.Notion.Models.Response.Comment;

public record ListCommentsResponse(CommentEntity[] Comments);