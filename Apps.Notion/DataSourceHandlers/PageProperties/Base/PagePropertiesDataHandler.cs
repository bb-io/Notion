using Apps.Notion.Api;
using Apps.Notion.Constants;
using Apps.Notion.Invocables;
using Apps.Notion.Models.Response.DataBase;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Notion.DataSourceHandlers.PageProperties.Base;

public abstract class PagePropertiesDataHandler : NotionInvocable, IAsyncDataSourceHandler
{
    protected abstract string[] Types { get; }
    private string DataBaseId { get; set; }

    public PagePropertiesDataHandler(InvocationContext invocationContext, string dataBaseId) : base(invocationContext)
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

        var response = await Client.ExecuteWithErrorHandling<DatabaseResponse>(request);

        return response.Properties.Values
            .Where(x => Types.Contains(x.Type) &&
                        x.Name.Contains(context.SearchString ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(x => x.Id, x => x.Name);
    }
}