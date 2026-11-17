using Apps.NotionOAuth.Api;
using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Models.Response;
using Apps.NotionOAuth.Models.Response.View;
using Blackbird.Applications.Sdk.Common.Authentication;
using RestSharp;

namespace Tests.Notion;

[TestClass]
public class NotionClientTests
{
    [TestMethod]
    public async Task QueryView_MultiplePages_UsesCachedQueryResultsEndpoint()
    {
        var client = new StubNotionClient(
            new CreateViewQueryResponse<ViewQueryResponse>
            {
                Id = "query-id",
                Results = [new() { Id = "page-1" }],
                NextCursor = "cursor-1",
                HasMore = true
            },
            new PaginationResponse<ViewQueryResponse>
            {
                Results = [new() { Id = "page-2" }],
                NextCursor = null,
                HasMore = false
            });
        var credentials = new[]
        {
            new AuthenticationCredentialsProvider(CredsNames.AccessToken, "token")
        };

        var result = await client.QueryView<ViewQueryResponse>("view-id", credentials);

        CollectionAssert.AreEqual(new[] { "page-1", "page-2" }, result.Select(x => x.Id).ToArray());
        Assert.AreEqual(3, client.Requests.Count);
        AssertRequest(client.Requests[0], Method.Post, "/views/view-id/queries");
        AssertRequest(client.Requests[1], Method.Get, "/views/view-id/queries/query-id");
        Assert.AreEqual(
            "cursor-1",
            client.Requests[1].Parameters.Single(x => x.Name == "start_cursor").Value);
        AssertRequest(client.Requests[2], Method.Delete, "/views/view-id/queries/query-id");
    }

    private static void AssertRequest(RestRequest request, Method method, string resource)
    {
        Assert.AreEqual(method, request.Method);
        Assert.AreEqual(resource, request.Resource);
    }

    private class StubNotionClient(params object[] responses) : NotionClient
    {
        private readonly Queue<object> _responses = new(responses);

        public List<RestRequest> Requests { get; } = [];

        public override Task<T> ExecuteWithErrorHandling<T>(RestRequest request)
        {
            Requests.Add(request);
            return Task.FromResult((T)_responses.Dequeue());
        }

        public override Task<RestResponse> ExecuteWithErrorHandling(RestRequest request)
        {
            Requests.Add(request);
            return Task.FromResult(new RestResponse(request));
        }
    }
}
