using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Entities;
using Apps.NotionOAuth.Models.Response.DataBase;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.NotionOAuth.DataSourceHandlers;

public class DatabaseDataHandler(InvocationContext invocationContext)
    : NotionInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var items = await Client.SearchAll<DatabaseResponse>(Creds, "database", context.SearchString, ApiConstants.NotLatestApiVersion);
        return items
            .Select(x => new DatabaseEntity(x))
            .OrderByDescending(x => x.CreatedTime)
            .DistinctBy(x => x.Id)
            .Select(x => new DataSourceItem(x.Id, x.Title));
    }
}