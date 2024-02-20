using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Entities;
using Apps.NotionOAuth.Models.Response.DataBase;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.NotionOAuth.DataSourceHandlers;

public class DatabaseDataHandler : NotionInvocable, IAsyncDataSourceHandler
{
    public DatabaseDataHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var items = await Client.SearchAll<DatabaseResponse>(Creds, "database", context.SearchString);

        return items.Select(x => new DatabaseEntity(x))
            .OrderByDescending(x => x.CreatedTime)
            .Take(30)
            .ToDictionary(x => x.Id, x => x.Title);
    }
}