using Apps.NotionOAuth.Api;
using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Models.Entities;
using Apps.NotionOAuth.Models.Response.Page;
using Apps.NotionOAuth.PollingEvents.Models.Requests;
using Apps.NotionOAuth.Utils;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.NotionOAuth.Services;

public class DatabaseService(InvocationContext invocationContext)
{
    private readonly NotionClient _client = new();

    public async Task<List<PageEntity>> QueryPagesInDatabase(QueryPagesInDatabaseRequest queryRequest)
    {
        PluginMisconfigurationExceptionHelper.ThrowIfNullOrEmpty(queryRequest.DatabaseId);
        PluginMisconfigurationExceptionHelper.ThrowIfNullOrEmpty(queryRequest.StatusPropertyName);
        PluginMisconfigurationExceptionHelper.ThrowIfNullOrEmpty(queryRequest.StatusPropertyType);
        PluginMisconfigurationExceptionHelper.ThrowIfNullOrEmpty(queryRequest.StatusPropertyValue);
        
        var endpoint = $"{ApiEndpoints.Databases}/{queryRequest.DatabaseId}/query";
        var request = new NotionRequest(endpoint, Method.Post, invocationContext.AuthenticationCredentialsProviders);

        var bodyDictionary = new Dictionary<string, object>()
        {
            ["filter"] = new Dictionary<string, object>
            {
                ["property"] = queryRequest.StatusPropertyName,
                [queryRequest.StatusPropertyType] = new
                {
                    equals = queryRequest.StatusPropertyValue
                }
            },
            ["sorts"] = new List<object>
            {
               new
               {
                   timestamp = "last_edited_time",
                   direction = "descending"
               }
            }
        };

        var response = await _client.PaginateWithBody<PageResponse>(request, bodyDictionary);   
        var pages = response
            .Select(x => new PageEntity(x))
            .ToList();

        return pages;
    }
}