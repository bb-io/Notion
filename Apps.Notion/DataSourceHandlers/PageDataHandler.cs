using Apps.Notion.Invocables;
using Apps.Notion.Models.Entities;
using Apps.Notion.Models.Response.Page;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Notion.DataSourceHandlers;

public class PageDataHandler : NotionInvocable, IAsyncDataSourceHandler
{
    public PageDataHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var items = await Client.SearchAll<PageResponse>(Creds, "page", context.SearchString);
       
        return items
            .Select(x => new PageEntity(x))
            .OrderByDescending(x => x.CreatedTime)
            .Take(30)
            .ToDictionary(x => x.Id, x => x.Title);
    }
}