using Apps.NotionOAuth.Api;
using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Response.DataBase;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.NotionOAuth.DataSourceHandlers.PageProperties.Base;

public abstract class PagePropertiesDataHandler(InvocationContext invocationContext, string dataBaseId)
    : NotionInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    protected abstract string[] Types { get; }
    private string DataBaseId { get; set; } = dataBaseId;

    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(DataBaseId))
            throw new("Please fill in the Database input first");

        var endpoint = $"{ApiEndpoints.Databases}/{DataBaseId}";
        var request = new NotionRequest(endpoint, Method.Get, Creds);

        var response = await Client.ExecuteWithErrorHandling<DatabaseResponse>(request);

        return response.Properties.Values
            .Where(x => Types.Contains(x.Type) &&
                        x.Name.Contains(context.SearchString ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            .Select(x => new DataSourceItem(x.Id, x.Name));
    }
}