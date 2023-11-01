using Apps.Notion.Api;
using Apps.Notion.Constants;
using Apps.Notion.Invocables;
using Apps.Notion.Models.Entities;
using Apps.Notion.Models.Request.User;
using Apps.Notion.Models.Response.User;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Notion.Actions;

[ActionList]
public class UserActions : NotionInvocable
{
    public UserActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    [Action("List users", Description = "List all users")]
    public async Task<ListUsersResponse> ListUsers()
    {
        var request = new NotionRequest(ApiEndpoints.Users, Method.Get, Creds);

        var response = await Client.Paginate<UserResponse>(request);
        var users = response.Select(x => new UserEntity(x)).ToArray();

        return new(users);
    }
    
    [Action("Get user", Description = "Get details of a specific user")]
    public async Task<UserEntity> GetUser([ActionParameter] UserRequest user)
    {
        var endpoint = $"{ApiEndpoints.Users}/{user.UserId}";
        var request = new NotionRequest(endpoint, Method.Get, Creds);

        var response = await Client.ExecuteWithErrorHandling<UserResponse>(request);
        return new(response);
    }
}