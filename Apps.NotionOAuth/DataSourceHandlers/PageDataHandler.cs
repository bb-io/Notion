using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Entities;
using Apps.NotionOAuth.Models.Response.Page;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.NotionOAuth.DataSourceHandlers;

public class PageDataHandler(InvocationContext invocationContext)
    : NotionInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var items = await Client.SearchAll<PageResponse>(Creds, "page", context.SearchString);
        return items
            .Select(x => new PageEntity(x))
            .OrderByDescending(x => x.CreatedTime)
            .Select(x =>new DataSourceItem(x.Id, x.Title));
    }
}