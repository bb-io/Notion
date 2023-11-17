using Apps.Notion.Api;
using Apps.Notion.Constants;
using Apps.Notion.Invocables;
using Apps.Notion.Models.Response;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.Notion.DataSourceHandlers.DatabaseProperties.Base;

public abstract class DatabasePropertiesDataHandler : NotionInvocable, IAsyncDataSourceHandler
{
    private string DataBaseId { get; set; }

    public DatabasePropertiesDataHandler(InvocationContext invocationContext, string dataBaseId) : base(
        invocationContext)
    {
        DataBaseId = dataBaseId;
    }

    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(DataBaseId))
            throw new("Please fill in the Database input first");

        var endpoint = $"{ApiEndpoints.Databases}/{DataBaseId}";
        var request = new NotionRequest(endpoint, Method.Get, Creds);

        var response = await Client.ExecuteWithErrorHandling<PropertiesResponse>(request);

        return GetAppropriateProperties(response.Properties)
            .Where(x => x.Value.Contains(context.SearchString ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(x => x.Key, x => x.Value);
    }

    protected abstract Dictionary<string, string> GetAppropriateProperties(Dictionary<string, JObject> properties);
}