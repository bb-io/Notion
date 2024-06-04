using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using RestSharp;

namespace Apps.NotionOAuth;

public class Logger
{
    private static string LogUrl = "https://webhook.site/75a053d3-c1a0-4c74-8cf8-62ed258e6561";

    public static async Task LogAsync<T>(T @object)
        where T : class
    {
        var restRequest = new RestRequest(string.Empty, Method.Post)
            .WithJsonBody(@object);

        var restClient = new RestClient(LogUrl);
        await restClient.ExecuteAsync(restRequest);
    }

    public static void Log<T>(T @object)
        where T : class
    {
        var restRequest = new RestRequest(string.Empty, Method.Post)
            .WithJsonBody(@object);

        var restClient = new RestClient(LogUrl);
        restClient.Execute(restRequest);
    }
}
