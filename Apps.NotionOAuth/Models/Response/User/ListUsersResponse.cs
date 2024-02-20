using Apps.NotionOAuth.Models.Entities;

namespace Apps.NotionOAuth.Models.Response.User;

public record ListUsersResponse(UserEntity[] Users);