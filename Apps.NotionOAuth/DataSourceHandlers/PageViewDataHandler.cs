using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Request.DataBase;
using Apps.NotionOAuth.Utils.Executor;
using Apps.NotionOAuth.Utils.Executor.Filters;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.NotionOAuth.DataSourceHandlers;

// Standard ViewDataHandler will not work here - it sends the data source ID, but the API doesn't support it for pages.
// But it works with just one database ID input
public class PageViewDataHandler : NotionInvocable, IAsyncDataSourceItemHandler
{
    private readonly ViewApiExecutor _apiExecutor;
    private readonly string _databaseId;

    public PageViewDataHandler(
        InvocationContext invocationContext,
        [ActionParameter] OptionalDatabaseRequest databaseInput) : base(invocationContext)
    {
        if (string.IsNullOrEmpty(databaseInput.DatabaseId))
            throw new PluginMisconfigurationException("Please specify the database ID input");
        
        _databaseId = databaseInput.DatabaseId;
        _apiExecutor = new ViewApiExecutor(invocationContext);
    }

    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken ct)
    {
        var filters = new SearchViewsFilter
        {
            DatabaseId = _databaseId,
            ViewNameContains = context.SearchString,
        };

        var views = await _apiExecutor.SearchViews(filters, ct);
        return views.Select(x => new DataSourceItem(x.Id, x.Name)).ToList();
    }
}
