using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Request.DataBase;
using Apps.NotionOAuth.Models.Request.DataSource;
using Apps.NotionOAuth.Utils.Executor;
using Apps.NotionOAuth.Utils.Executor.Filters;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.NotionOAuth.DataSourceHandlers;

public class ViewDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] OptionalDatabaseRequest databaseInput,
    [ActionParameter] OptionalDataSourceRequest dataSourceInput) 
    : NotionInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    private readonly ViewApiExecutor _apiExecutor = new(invocationContext);

    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(databaseInput.DatabaseId) && string.IsNullOrEmpty(dataSourceInput.DataSourceId))
            throw new PluginMisconfigurationException("Please specify either database ID or data source ID");

        var filters = new SearchViewsFilter 
        { 
            DatabaseId = databaseInput.DatabaseId,
            DataSourceId = dataSourceInput.DataSourceId,
            ViewNameContains = context.SearchString,
        };

        var views = await _apiExecutor.SearchViews(filters, ct);
        return views.Select(x => new DataSourceItem(x.Id, x.Name)).ToList();
    }
}
