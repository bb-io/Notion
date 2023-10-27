using Apps.Notion.Invocables;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Notion.DataSourceHandlers;

public class DatabaseDataHandler : NotionInvocable, IAsyncDataSourceHandler
{
    public DatabaseDataHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}