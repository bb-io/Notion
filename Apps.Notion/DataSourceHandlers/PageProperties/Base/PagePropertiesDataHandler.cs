using Apps.Notion.Api;
using Apps.Notion.Constants;
using Apps.Notion.Invocables;
using Apps.Notion.Models.Response;
using Apps.Notion.Models.Response.Page;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Notion.DataSourceHandlers.PageProperties.Base;

public abstract class PagePropertiesDataHandler : NotionInvocable, IAsyncDataSourceHandler
{
    protected abstract string[] Types { get; }
    private string PageId { get; set; }

    public PagePropertiesDataHandler(InvocationContext invocationContext, string pageId) : base(invocationContext)
    {
        PageId = pageId;
    }

    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(PageId))
            throw new("Please fill in the Page input first");
        
        var endpoint = $"{ApiEndpoints.Pages}/{PageId}";
        var request = new NotionRequest(endpoint, Method.Get, Creds);

        var response = await Client.ExecuteWithErrorHandling<PageResponse>(request);

        return response.Properties
            .Select(x => new PropertyResponse(x))
            .Where(x => Types.Contains(x.Type) &&
                        x.Name.Contains(context.SearchString ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(x => x.Id, x => x.Name);
    }
}