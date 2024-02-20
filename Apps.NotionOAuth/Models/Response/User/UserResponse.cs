namespace Apps.NotionOAuth.Models.Response.User;

public class UserResponse
{
    public string Id { get; set; }
    
    public string Name { get; set; }
    
    public string? AvatarUrl { get; set; }
    
    public PersonResponse Person { get; set; }
}