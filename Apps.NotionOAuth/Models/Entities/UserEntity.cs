using Apps.NotionOAuth.Models.Response.User;
using Blackbird.Applications.Sdk.Common;

namespace Apps.NotionOAuth.Models.Entities;

public class UserEntity
{
    [Display("User ID")] public string Id { get; set; }

    public string Name { get; set; }

    [Display("Mention user")]
    public string MentionUser => string.IsNullOrWhiteSpace(Id)
        ? string.Empty
        : $"<mention-user url=\"{Id}\">{Name}</mention-user>";

    [Display("Avatar URL")] public string? AvatarUrl { get; set; }

    public string? Email { get; set; }

    public UserEntity(UserResponse response)
    {
        Id = response.Id;
        Name = response.Name;
        AvatarUrl = response.AvatarUrl;
        Email = response.Person?.Email;
    }
}
