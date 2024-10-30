using Apps.NotionOAuth.Api;
using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Entities;
using Apps.NotionOAuth.Models.Request.User;
using Apps.NotionOAuth.Models.Response.User;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.NotionOAuth.Actions;

[ActionList]
public class UserActions(InvocationContext invocationContext) : NotionInvocable(invocationContext)
{
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