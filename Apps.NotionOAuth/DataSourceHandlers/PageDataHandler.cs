using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Entities;
using Apps.NotionOAuth.Models.Response.Page;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.NotionOAuth.DataSourceHandlers;

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