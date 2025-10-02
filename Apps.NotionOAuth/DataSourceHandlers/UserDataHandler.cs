using Apps.NotionOAuth.Api;
using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Response.User;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.NotionOAuth.DataSourceHandlers;

public class UserDataHandler(InvocationContext invocationContext)
    : NotionInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var request = new NotionRequest(ApiEndpoints.Users, Method.Get, Creds);
        var response = await Client.Paginate<UserResponse>(request);
        return response
            .Where(x => context.SearchString is null ||
                        x.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .DistinctBy(x => x.Id)
            .Select(x => new DataSourceItem(x.Id, x.Name));
    }
}