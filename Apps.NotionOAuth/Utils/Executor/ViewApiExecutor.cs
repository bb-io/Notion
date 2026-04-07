using Apps.NotionOAuth.Api;
using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Response.View;
using Apps.NotionOAuth.Utils.Executor.Filters;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;
using System.Collections.Concurrent;

namespace Apps.NotionOAuth.Utils.Executor;

public class ViewApiExecutor(InvocationContext invocationContext) : NotionInvocable(invocationContext)
{
    public async Task<IEnumerable<ViewResponse>> SearchViews(
        SearchViewsFilter filter, 
        CancellationToken cancellationToken = default)
    {
        var request = new NotionRequest(ApiEndpoints.Views, Method.Get, Creds);

        if (!string.IsNullOrEmpty(filter.DatabaseId))
            request.AddQueryParameter("database_id", filter.DatabaseId);

        if (!string.IsNullOrEmpty(filter.DataSourceId))
            request.AddQueryParameter("data_source_id", filter.DataSourceId);

        var views = await Client.Paginate<ViewResponse>(request);
        var fullViews = new ConcurrentBag<ViewResponse>();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 3,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(views, parallelOptions, async (minimalView, token) =>
        {
            var viewRequest = new NotionRequest($"{ApiEndpoints.Views}/{minimalView.Id}", Method.Get, Creds);

            var fullView = await Client.ExecuteWithErrorHandling<ViewResponse>(viewRequest);
            if (string.IsNullOrEmpty(filter.ViewNameContains) ||
                fullView.Name.Contains(filter.ViewNameContains, StringComparison.OrdinalIgnoreCase))
                fullViews.Add(fullView);
        });

        return fullViews;
    }
}
