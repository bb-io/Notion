using Apps.NotionOAuth.Api;
using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Request.DataBase;
using Apps.NotionOAuth.Models.Request.DataSource;
using Apps.NotionOAuth.Models.Request.View;
using Apps.NotionOAuth.Models.Response.View;
using Apps.NotionOAuth.Utils.Executor;
using Apps.NotionOAuth.Utils.Executor.Filters;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.NotionOAuth.Actions;

[ActionList("Views")]
public class ViewActions(InvocationContext invocationContext) : NotionInvocable(invocationContext)
{
    private readonly ViewApiExecutor _apiExecutor = new(invocationContext);

    [Action("Search views", Description = "Search all views in a database")]
    public async Task<SearchViewsResponse> SearchViews(
        [ActionParameter] OptionalDatabaseRequest databaseInput,
        [ActionParameter] OptionalDataSourceRequest dataSourceInput,
        [ActionParameter] SearchViewsRequest searchInput)
    {
        if (string.IsNullOrEmpty(databaseInput.DatabaseId) && string.IsNullOrEmpty(dataSourceInput.DataSourceId))
            throw new PluginMisconfigurationException("Please specify either database ID or data source ID");

        var filter = new SearchViewsFilter
        {
            DatabaseId = databaseInput.DatabaseId,
            DataSourceId = dataSourceInput.DataSourceId,
            ViewNameContains = searchInput.ViewNameContains,
        };
        var views = await _apiExecutor.SearchViews(filter);
        return new(views.ToList());
    }

    [Action("Get view", Description = "Get details for a specific view")]
    public async Task<ViewResponse> GetView([ActionParameter] ViewRequest viewInput)
    {
        var request = new NotionRequest($"{ApiEndpoints.Views}/{viewInput.ViewId}", Method.Get, Creds);
        var view = await Client.ExecuteWithErrorHandling<ViewResponse>(request);
        return view;
    }

    [Action("Create view", Description = "Create a new view on a database")]
    public async Task<ViewResponse> CreateView(
        [ActionParameter] DataSourceRequest dataSourceInput,
        [ActionParameter] DatabaseRequest databaseInput,
        [ActionParameter] CreateViewRequest createViewInput)
    {
        var body = new Dictionary<string, object?>
        {
            { "data_source_id", dataSourceInput.DataSourceId },
            { "name", createViewInput.Name },
            { "type", createViewInput.Type },
            { "database_id", databaseInput.DatabaseId }
        };

        var request = new NotionRequest("views", Method.Post, Creds).AddJsonBody(body);
        return await Client.ExecuteWithErrorHandling<ViewResponse>(request);
    }

    [Action("Delete view", Description = "Delete a view from a database")]
    public async Task DeleteView([ActionParameter] ViewRequest viewInput)
    {
        var request = new NotionRequest($"views/{viewInput.ViewId}", Method.Delete, Creds);
        await Client.ExecuteWithErrorHandling(request);
    }
}
