using Apps.Notion.Invocables;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Notion.DataSourceHandlers;

public class PageDataHandler : NotionInvocable, IAsyncDataSourceHandler
{
    public PageDataHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}