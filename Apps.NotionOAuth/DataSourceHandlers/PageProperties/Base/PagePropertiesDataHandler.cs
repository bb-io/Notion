using Apps.NotionOAuth.Api;
using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Response.DataBase;
using Apps.NotionOAuth.Models.Response.Page;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.NotionOAuth.DataSourceHandlers.PageProperties.Base;

public abstract class PagePropertiesDataHandler(InvocationContext invocationContext, string dataBaseId, string? pageId = null)
    : NotionInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    protected abstract string[] Types { get; }
    private string DataBaseId { get; } = dataBaseId;
    private string? PageId { get; } = pageId;

    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(DataBaseId) && string.IsNullOrWhiteSpace(PageId))
        {
            throw new InvalidOperationException("Please provide either a Database ID or Page ID.");
        }

        var pageProperties = string.IsNullOrEmpty(PageId)
            ? new List<DataSourceItem>()
            : await FetchPagePropertiesAsync(context);

        var databaseProperties = string.IsNullOrEmpty(DataBaseId)
            ? new List<DataSourceItem>()
            : await FetchDatabasePropertiesAsync(context);

        if (!string.IsNullOrEmpty(DataBaseId) && !string.IsNullOrEmpty(PageId))
        {
            pageProperties = pageProperties.Select(prop => new DataSourceItem(prop.Value, $"[page] {prop.DisplayName}"))
                .ToList();
            databaseProperties = databaseProperties
                .Select(prop => new DataSourceItem(prop.Value, $"[database] {prop.DisplayName}")).ToList();
        }

        var combinedProperties = new List<DataSourceItem>();
        combinedProperties.AddRange(databaseProperties);
        combinedProperties.AddRange(pageProperties);

        return combinedProperties.DistinctBy(prop => prop.Value).ToList();
    }

    private async Task<List<DataSourceItem>> FetchPagePropertiesAsync(DataSourceContext context)
    {
        var pageEndpoint = $"{ApiEndpoints.Pages}/{PageId}";
        var request = new NotionRequest(pageEndpoint, Method.Get, Creds);

        var response = await Client.ExecuteWithErrorHandling<PageResponse>(request);

        return response.Properties?
            .Where(prop => Types.Contains(prop.Value["type"]?.ToString()) &&
                           prop.Key.Contains(context.SearchString ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            .Where(prop => prop.Value["id"]?.ToString() != null)
            .Select(prop => new DataSourceItem(prop.Value["id"]!.ToString(), prop.Key))
            .ToList() ?? new List<DataSourceItem>();
    }

    private async Task<List<DataSourceItem>> FetchDatabasePropertiesAsync(DataSourceContext context)
    {
        var databaseEndpoint = $"{ApiEndpoints.Databases}/{DataBaseId}";
        var request = new NotionRequest(databaseEndpoint, Method.Get, Creds);

        var response = await Client.ExecuteWithErrorHandling<DatabaseResponse>(request);

        return response.Properties.Values
            .Where(prop => Types.Contains(prop.Type) &&
                           prop.Name.Contains(context.SearchString ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            .Select(prop => new DataSourceItem(prop.Id, prop.Name))
            .ToList();
    }
}