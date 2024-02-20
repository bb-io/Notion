using Apps.NotionOAuth.Models.Entities;

namespace Apps.NotionOAuth.Models.Response.Comment;

public record ListCommentsResponse(CommentEntity[] Comments);