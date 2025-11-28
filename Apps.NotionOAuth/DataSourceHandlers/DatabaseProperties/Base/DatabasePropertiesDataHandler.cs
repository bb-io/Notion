using Apps.NotionOAuth.Api;
using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Response;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.NotionOAuth.DataSourceHandlers.DatabaseProperties.Base;

public abstract class DatabasePropertiesDataHandler(InvocationContext invocationContext, string dataBaseId, string dataSourceId)
    : NotionInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    private string DataBaseId { get; set; } = dataBaseId;
    private string DataSourceId { get; set; } = dataSourceId;

    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        NotionRequest request;

        if (!string.IsNullOrWhiteSpace(DataBaseId))
            request = new NotionRequest($"{ApiEndpoints.Databases}/{DataBaseId}", Method.Get, Creds, ApiConstants.NotLatestApiVersion);
        else if (!string.IsNullOrWhiteSpace(DataSourceId))
            request = new NotionRequest($"{ApiEndpoints.DataSources}/{DataSourceId}", Method.Get, Creds, ApiConstants.LatestApiVersion);
        else
            throw new Exception("Please provide 'Database ID' or 'Datasource ID' input first.");

        var response = await Client.ExecuteWithErrorHandling<PropertiesResponse>(request);

        return GetAppropriateProperties(response.Properties)
            .Where(x => x.Value.Contains(context.SearchString ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            .DistinctBy(x => x.Key)
            .Select(x => new DataSourceItem(x.Key, x.Value));
    }

    protected abstract Dictionary<string, string> GetAppropriateProperties(Dictionary<string, JObject> properties);
}