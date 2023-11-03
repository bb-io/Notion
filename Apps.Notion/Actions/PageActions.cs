using System.Net.Mime;
using System.Text;
using Apps.Notion.Api;
using Apps.Notion.Constants;
using Apps.Notion.Invocables;
using Apps.Notion.Models.Entities;
using Apps.Notion.Models.Request;
using Apps.Notion.Models.Request.Block;
using Apps.Notion.Models.Request.Page;
using Apps.Notion.Models.Response;
using Apps.Notion.Models.Response.Page;
using Apps.Notion.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using Newtonsoft.Json.Linq;
using RestSharp;
using File = Blackbird.Applications.Sdk.Common.Files.File;

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
        if (input.PageId is not null && input.DatabaseId is not null)
            throw new("Page cannot have two parents, you should specify either parent page or parent database");

        if (input.PageId is null && input.DatabaseId is null)
            throw new("Page must have a parent, you should specify either parent page or parent database");

        var request = new NotionRequest(ApiEndpoints.Pages, Method.Post, Creds)
            .WithJsonBody(new CreatePageRequest(input), JsonConfig.Settings);

        var response = await Client.ExecuteWithErrorHandling<PageResponse>(request);
        return new(response);
    }

    [Action("Create page from HTML", Description = "Create a new page from HTML")]
    public async Task<PageEntity> CreatePageFromHtml(
        [ActionParameter] CreatePageInput input,
        [ActionParameter] FileRequest file)
    {
        var page = await CreatePage(input);

        var blocks = NotionHtmlParser.ParseHtml(file.File.Bytes);
        await new BlockActions(InvocationContext).AppendBlockChildren(page.Id, blocks);

        return page;
    }

    [Action("Get page as HTML", Description = "Get content of a specific page as HTML")]
    public async Task<FileResponse> GetPageAsHtml(
        [ActionParameter] PageRequest page)
    {
        var endpoint = $"{ApiEndpoints.Blocks}/{page.PageId}/children";
        var request = new NotionRequest(endpoint, Method.Get, Creds);

        var response = await Client.Paginate<JObject>(request);
        var html = NotionHtmlParser.ParseBlocks(response.ToArray());

        return new()
        {
            File = new(Encoding.UTF8.GetBytes(html))
            {
                Name = $"{page.PageId}.html",
                ContentType = MediaTypeNames.Text.Html
            }
        };
    }

    [Action("Update page from HTML", Description = "Update specific page from an HTML file")]
    public async Task UpdatePageFromHtml(
        [ActionParameter] PageRequest page,
        [ActionParameter] FileRequest file)
    {
        var actions = new BlockActions(InvocationContext);
        var children = await actions.ListBlockChildren(new()
        {
            BlockId = page.PageId
        });

        // Can't remove all blocks in parallel, because it can cause rate limiting errors
        foreach (var child in children.Children)
        {
            try
            {
                await actions.DeleteBlock(new()
                {
                    BlockId = child.Id
                });
            }
            catch
            {
                // ignored
            }
        }

        var blocks = NotionHtmlParser.ParseHtml(file.File.Bytes);
        await actions.AppendBlockChildren(page.PageId, blocks);
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
                archived = true
            });

        return Client.ExecuteWithErrorHandling(request);
    }
}