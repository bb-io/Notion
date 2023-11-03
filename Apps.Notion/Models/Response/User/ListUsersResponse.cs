using Apps.Notion.Models.Entities;

namespace Apps.Notion.Models.Response.User;

public record ListUsersResponse(UserEntity[] Users);