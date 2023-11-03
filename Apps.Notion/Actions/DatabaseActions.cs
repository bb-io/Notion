using Apps.Notion.Api;
using Apps.Notion.Constants;
using Apps.Notion.Invocables;
using Apps.Notion.Models.Entities;
using Apps.Notion.Models.Request;
using Apps.Notion.Models.Request.DataBase;
using Apps.Notion.Models.Response.DataBase;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using RestSharp;

namespace Apps.Notion.Actions;

[ActionList]
public class DatabaseActions : NotionInvocable
{
    public DatabaseActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    [Action("List databases", Description = "List all databases")]
    public async Task<ListDatabasesResponse> ListDatabases()
    {
        var items = await Client.SearchAll<DatabaseResponse>(Creds, "database");
        var databases = items.Select(x => new DatabaseEntity(x)).ToArray();

        return new(databases);
    }

    [Action("Create database", Description = "Create a new database")]
    public async Task<DatabaseEntity> CreateDatabase([ActionParameter] CreateDatabaseInput input)
    {
        input.Properties ??= new List<PropertyRequest>();
        if (input.Properties.All(x => x.Type != "title"))
        {
            var mandatoryProperties = new List<PropertyRequest>()
            {
                new()
                {
                    Name = "Name",
                    Type = "title"
                }
            };
            input.Properties = input.Properties.Concat(mandatoryProperties);
        }

        var request = new NotionRequest(ApiEndpoints.Databases, Method.Post, Creds)
            .WithJsonBody(new CreateDatabaseRequest(input), JsonConfig.Settings);

        var response = await Client.ExecuteWithErrorHandling<DatabaseResponse>(request);
        return new(response);
    }

    [Action("Get database", Description = "Get details of a specific database")]
    public async Task<DatabaseEntity> GetDatabase([ActionParameter] DatabaseRequest input)
    {
        var endpoint = $"{ApiEndpoints.Databases}/{input.DatabaseId}";
        var request = new NotionRequest(endpoint, Method.Get, Creds);

        var response = await Client.ExecuteWithErrorHandling<DatabaseResponse>(request);
        return new(response);
    }
}