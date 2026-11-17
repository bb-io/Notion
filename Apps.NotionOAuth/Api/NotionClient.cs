using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Models.Request;
using Apps.NotionOAuth.Models.Response;
using Apps.NotionOAuth.Models.Response.View;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using Blackbird.Applications.Sdk.Utils.Extensions.String;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.NotionOAuth.Api;

public class NotionClient() : BlackBirdRestClient(new()
{
    BaseUrl = Urls.Api.ToUri(),
    MaxTimeout = 180000
})
{
    protected override JsonSerializerSettings? JsonSettings => JsonConfig.Settings;

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        try
        {
            if (!string.IsNullOrEmpty(response.Content) &&
                response.Content.Trim().Contains("<html", StringComparison.OrdinalIgnoreCase))
            {
                throw new PluginApplicationException($"Returned HTML content instead of JSON: {response.Content}");
            }

            var error = JsonConvert.DeserializeObject<ErrorResponse>(response.Content!, JsonSettings);
            if (error == null)
            {
                throw new PluginApplicationException($"Could not parse {response.Content} to {typeof(ErrorResponse)}");
            }
            
            return new PluginApplicationException(error.Message);
        }
        catch (Exception ex) when (ex is JsonReaderException ||
                                ex is JsonSerializationException ||
                                ex is ArgumentNullException)
        {
            throw new PluginApplicationException($"Failed to process server response: {ex.Message} Content: {response.Content}");
        }
    }

    public async Task<List<T>> SearchAll<T>(AuthenticationCredentialsProvider[] creds, string type, string? query = null, string? apiVersion = null)
    {
        string? cursor = null;

        var results = new List<T>();
        do
        {
            var request = new NotionRequest(ApiEndpoints.Search, Method.Post, creds, apiVersion)
                .WithJsonBody(new FilterRequest(type, cursor, query), JsonSettings);
            var response = await ExecuteWithErrorHandling<PaginationResponse<T>>(request);

            results.AddRange(response.Results);
            cursor = response.NextCursor;
        } while (cursor is not null);

        return results;
    }
    
    public async Task<List<T>> Paginate<T>(RestRequest request)
    {
        string? cursor = null;
        var baseUrl = request.Resource;
        
        var results = new List<T>();
        do
        {
            if (cursor is not null)
            {
                request.Resource = baseUrl.SetQueryParameter("start_cursor", cursor);
            }
            
            var response = await ExecuteWithErrorHandling<PaginationResponse<T>>(request);

            results.AddRange(response.Results);
            cursor = response.NextCursor;
        } while (cursor is not null);

        return results;
    }

    public async Task<List<T>> PaginateWithBody<T>(RestRequest request, Dictionary<string, object>? body = null, int? maxPagesCount = null)
    {
        string? cursor = null;
        int pageCount = 0;
        
        if (body != null)
        {
            request.AddJsonBody(body);
        }
        
        var results = new List<T>();
        do
        {
            if (cursor is not null)
            {
                var existingBody = request.Parameters.FirstOrDefault(p => p.Type == ParameterType.RequestBody);
                if (existingBody != null)
                    request.Parameters.RemoveParameter(existingBody);

                if (body != null)
                {
                    body["start_cursor"] = cursor;
                    request.WithJsonBody(body);
                }
                else
                {
                    request.AddJsonBody(new { start_cursor = cursor });
                }
            }

            var response = await ExecuteWithErrorHandling<PaginationResponse<T>>(request);

            results.AddRange(response.Results);
            cursor = response.NextCursor;
            pageCount++;
        } while (cursor is not null && (maxPagesCount is null || pageCount < maxPagesCount));

        return results;
    }

    public async Task<List<T>> QueryView<T>(
        string viewId,
        AuthenticationCredentialsProvider[] creds)
    {
        var createRequest = new NotionRequest($"{ApiEndpoints.Views}/{viewId}/queries", Method.Post, creds);
        var query = await ExecuteWithErrorHandling<CreateViewQueryResponse<T>>(createRequest);
        var results = query.Results.ToList();

        try
        {
            var cursor = query.NextCursor;
            while (cursor is not null)
            {
                var pageRequest = new NotionRequest(
                    $"{ApiEndpoints.Views}/{viewId}/queries/{query.Id}",
                    Method.Get,
                    creds);
                pageRequest.AddQueryParameter("start_cursor", cursor);

                var page = await ExecuteWithErrorHandling<PaginationResponse<T>>(pageRequest);
                results.AddRange(page.Results);
                cursor = page.NextCursor;
            }
        }
        finally
        {
            await DeleteViewQuery(viewId, query.Id, creds);
        }

        return results;
    }

    private async Task DeleteViewQuery(
        string viewId,
        string queryId,
        AuthenticationCredentialsProvider[] creds)
    {
        if (string.IsNullOrEmpty(queryId))
            return;

        try
        {
            var deleteRequest = new NotionRequest(
                $"{ApiEndpoints.Views}/{viewId}/queries/{queryId}",
                Method.Delete,
                creds);
            await ExecuteWithErrorHandling(deleteRequest);
        }
        catch
        {
            // Query cleanup is best-effort; cached view queries expire automatically.
        }
    }

    public override async Task<RestResponse> ExecuteWithErrorHandling(RestRequest request)
    {
        RestResponse restResponse = await ExecuteAsync(request);
        if (!restResponse.IsSuccessStatusCode)
        {
            throw ConfigureErrorException(restResponse);
        }

        return restResponse;
    }

    public override async Task<T> ExecuteWithErrorHandling<T>(RestRequest request)
    {
        string content = (await ExecuteWithErrorHandling(request)).Content;

        T val = JsonConvert.DeserializeObject<T>(content, JsonSettings);
        if (val == null)
        {
            throw new PluginApplicationException($"Could not parse {content} to {typeof(T)}");
        }
        return val;
    }
}