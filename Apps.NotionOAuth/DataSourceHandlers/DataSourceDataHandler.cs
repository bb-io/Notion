using Apps.NotionOAuth.Api;
using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Entities;
using Apps.NotionOAuth.Models.Request.DataBase;
using Apps.NotionOAuth.Models.Response.DataBase;
using Apps.NotionOAuth.Models.Response.DataSource;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.NotionOAuth.DataSourceHandlers;

public class DataSourceDataHandler(InvocationContext invocationContext, [ActionParameter] DatabaseRequest request)
    : NotionInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.DatabaseId))
        {
            var endpoint = $"{ApiEndpoints.Databases}/{request.DatabaseId}";
            var apiRequest = new NotionRequest(endpoint, Method.Get, Creds, ApiConstants.LatestApiVersion);

            var databaseResponse = await Client.ExecuteWithErrorHandling<DatabaseResponse>(apiRequest);
            return databaseResponse.DataSources
                .Where(x => context.SearchString == null || x.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
                .Select(x => new DataSourceItem(x.Id, x.Name));
        }
        
        var items = await Client.SearchAll<DataSourceResponse>(Creds, "data_source", context.SearchString);
        return items
            .Select(x => new DataSourceEntity(x))
            .OrderByDescending(x => x.CreatedTime)
            .Select(x => new DataSourceItem(x.Id, x.Title));
    }
}