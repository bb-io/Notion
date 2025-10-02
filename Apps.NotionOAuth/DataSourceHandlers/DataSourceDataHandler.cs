using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Entities;
using Apps.NotionOAuth.Models.Response.DataSource;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.NotionOAuth.DataSourceHandlers;

public class DataSourceDataHandler(InvocationContext invocationContext): NotionInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var items = await Client.SearchAll<DataSourceResponse>(Creds, "data_source", context.SearchString);
        return items
            .Select(x => new DataSourceEntity(x))
            .OrderByDescending(x => x.CreatedTime)
            .DistinctBy(x => x.Id)
            .Select(x => new DataSourceItem(x.Id, x.Title));
    }
}