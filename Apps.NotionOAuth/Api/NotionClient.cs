using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Models.Request;
using Apps.NotionOAuth.Models.Response;
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
    BaseUrl = Urls.Api.ToUri()
})
{
    protected override JsonSerializerSettings? JsonSettings => JsonConfig.Settings;

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        try
        {
            var error = JsonConvert.DeserializeObject<ErrorResponse>(response.Content!, JsonSettings);
            if (error == null)
            {
                throw new Exception($"Could not parse {response.Content} to {typeof(ErrorResponse)}");
            }
            return new PluginApplicationException(error.Message);
        }
        catch (JsonReaderException ex)
        {
            throw new Exception($"Error reading JSON: {ex.Message} Content: {response.Content}", ex);
        }
        catch (JsonSerializationException ex)
        {
            throw new Exception($"Error deserializing JSON: {ex.Message} Content: {response.Content}", ex);
        }
        catch (ArgumentNullException ex)
        {
            throw new Exception($"Input JSON string is null: {ex.Message}", ex);
        }
    }

    public async Task<List<T>> SearchAll<T>(AuthenticationCredentialsProvider[] creds, string type, string? query = null)
    {
        string? cursor = null;

        var results = new List<T>();
        do
        {
            var request = new NotionRequest(ApiEndpoints.Search, Method.Post, creds)
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
                if(body != null)
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
    public virtual async Task<RestResponse> ExecuteWithErrorHandling(RestRequest request)
    {
        RestResponse restResponse = await ExecuteAsync(request);
        if (!restResponse.IsSuccessStatusCode)
        {
            throw ConfigureErrorException(restResponse);
        }

        return restResponse;
    }

    public virtual async Task<T> ExecuteWithErrorHandling<T>(RestRequest request)
    {
        string content = (await ExecuteWithErrorHandling(request)).Content;
        try
        {
            T val = JsonConvert.DeserializeObject<T>(content, JsonSettings);
            if (val == null)
            {
                throw new Exception($"Could not parse {content} to {typeof(T)}");
            }
            return val;
        }
        catch (JsonReaderException ex)
        {
            throw new Exception($"Error reading JSON: {ex.Message} Content: {content}", ex);
        }
        catch (JsonSerializationException ex)
        {
            throw new Exception($"Error deserializing JSON: {ex.Message} Content: {content}", ex);
        }
        catch (ArgumentNullException ex)
        {
            throw new Exception($"Input JSON string is null: {ex.Message}", ex);
        }
    }
}