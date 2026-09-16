using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Request.DataBase;
using Apps.NotionOAuth.Utils.Executor;
using Apps.NotionOAuth.Utils.Executor.Filters;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.NotionOAuth.DataSourceHandlers;

// Standard ViewDataHandler does not work with pages - it sends the data source ID, but the API doesn't support it for pages.
// But it works with just one database ID input, which is now required
public class PageViewDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] DatabaseRequest databaseInput) 
    : NotionInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    private readonly ViewApiExecutor _apiExecutor = new(invocationContext);

    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(databaseInput.DatabaseId))
            throw new PluginMisconfigurationException("Please specify the database ID input");

        var filters = new SearchViewsFilter
        {
            DatabaseId = databaseInput.DatabaseId,
            ViewNameContains = context.SearchString,
        };

        var views = await _apiExecutor.SearchViews(filters, ct);
        return views.Select(x => new DataSourceItem(x.Id, x.Name)).ToList();
    }
}
