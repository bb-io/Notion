using Apps.Notion.Api;
using Apps.Notion.Constants;
using Apps.Notion.Invocables;
using Apps.Notion.Models.Entities;
using Apps.Notion.Models.Request.Page;
using Apps.Notion.Models.Response.Page;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using RestSharp;

namespace Apps.Notion.Actions;

[ActionList]
public class PageActions : NotionInvocable
{
    public PageActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    [Action("List pages", Description = "List all pages")]
    public async Task<ListPagesResponse> ListPages()
    {
        var items = await Client.SearchAll<PageResponse>(Creds, "page");
        var pages = items.Select(x => new PageEntity(x)).ToArray();
        
        return new(pages);
    }
    
    [Action("Create page", Description = "Create a new page")]
    public async Task<PageEntity> CreatePage([ActionParameter] CreatePageInput input)
    {
        var request = new NotionRequest(ApiEndpoints.Pages, Method.Post, Creds)
            .WithJsonBody(new CreatePageRequest(input), JsonConfig.Settings);

        var response = await Client.ExecuteWithErrorHandling<PageResponse>(request);
        return new(response);
    }

    [Action("Get page", Description = "Get details of a specific page")]
    public async Task<PageEntity> GetPage([ActionParameter] PageRequest input)
    {
        var endpoint = $"{ApiEndpoints.Pages}/{input.PageId}";
        var request = new NotionRequest(endpoint, Method.Get, Creds);

        var response = await Client.ExecuteWithErrorHandling<PageResponse>(request);
        return new(response);
    }

    [Action("Archive page", Description = "Archive a specific page")]
    public Task ArchivePage([ActionParameter] PageRequest input)
    {
        var endpoint = $"{ApiEndpoints.Pages}/{input.PageId}";
        var request = new NotionRequest(endpoint, Method.Patch, Creds)
            .WithJsonBody(new
            {
                archive = true
            });

        return Client.ExecuteWithErrorHandling(request);
    }
}