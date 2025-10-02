using Apps.NotionOAuth.Api;
using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Response;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.NotionOAuth.DataSourceHandlers.DatabaseProperties.Base;

public abstract class DatabasePropertiesDataHandler(InvocationContext invocationContext, string dataBaseId)
    : NotionInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    private string DataBaseId { get; set; } = dataBaseId;

    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(DataBaseId))
            throw new("Please fill in the Database input first");

        var endpoint = $"{ApiEndpoints.Databases}/{DataBaseId}";
        var request = new NotionRequest(endpoint, Method.Get, Creds);

        var response = await Client.ExecuteWithErrorHandling<PropertiesResponse>(request);

        return GetAppropriateProperties(response.Properties)
            .Where(x => x.Value.Contains(context.SearchString ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            .DistinctBy(x => x.Key)
            .Select(x => new DataSourceItem(x.Key, x.Value));
    }

    protected abstract Dictionary<string, string> GetAppropriateProperties(Dictionary<string, JObject> properties);
}