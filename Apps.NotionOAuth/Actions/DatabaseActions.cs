using Apps.NotionOAuth.Api;
using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Extensions;
using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Entities;
using Apps.NotionOAuth.Models.Request;
using Apps.NotionOAuth.Models.Request.DataBase;
using Apps.NotionOAuth.Models.Request.DataBase.Properties.Getters;
using Apps.NotionOAuth.Models.Response.DataBase;
using Apps.NotionOAuth.Models.Response.Page;
using Apps.NotionOAuth.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.NotionOAuth.Actions;

[ActionList]
public class DatabaseActions(InvocationContext invocationContext) : NotionInvocable(invocationContext)
{
    [Action("Search databases", Description = "Searches databases based on specified parameters")]
    public async Task<ListDatabasesResponse> ListDatabases([ActionParameter] ListRequest input)
    {
        var items = await Client.SearchAll<DatabaseResponse>(Creds, "database");
        var databases = items
            .Select(x => new DatabaseEntity(x))
            .Where(x => x.LastEditedTime > (input.EditedSince ?? default))
            .Where(x => x.CreatedTime > (input.CreatedSince ?? default))
            .ToArray();

        return new(databases);
    }

    [Action("Search pages in database (deprecated)", Description = "Search pages in database that match specific condition. Use 'Search pages in data source' instead")]
    public async Task<ListPagesResponse> SearchPagesInDatabase([ActionParameter] SearchPagesInDatabaseRequest input)
    {
        var endpoint = $"{ApiEndpoints.Databases}/{input.DatabaseId}/query";
        var request = new NotionRequest(endpoint, Method.Post, Creds);
        Dictionary<string, object>? bodyDictionary = null;

        if(input.FilterProperty != null && input.FilterPropertyType != null)
        {
            if(input.FilterValue == null && input.FilterValueIsEmpty == null)
                throw new("'Filter value' or 'Filter value must be empty' must be provided");
            
            var filterValueDict = new Dictionary<string, object>();
            
            if (input.FilterValueIsEmpty != null)
            {
                filterValueDict["is_not_empty"] = !input.FilterValueIsEmpty;
            }

            if (input.FilterValue != null)
            {
                filterValueDict["equals"] = input.FilterValue;
            }
            
            bodyDictionary = new Dictionary<string, object>
            {
                ["filter"] = new Dictionary<string, object>
                {
                    ["property"] = input.FilterProperty,
                    [input.FilterPropertyType] = filterValueDict
                }
            };
        }

        var response = await Client.PaginateWithBody<PageResponse>(request, bodyDictionary);
        var pages = response
            .Where(x => x.LastEditedTime > (input.EditedSince ?? default))
            .Where(x => x.CreatedTime > (input.CreatedSince ?? default))
            .Where(x => input.CheckboxProperty is null || x.FilterCheckboxProperty(input.CheckboxProperty))
            .Where(x => input.SelectProperty is null || x.FilterSelectProperty(input.SelectProperty))
            .Where(x => input.PropertiesShouldHaveValue is null || input.PropertiesShouldHaveValue.All(x.PagePropertyHasValue))
            .Where(x => input.PropertiesWithoutValues is null || input.PropertiesWithoutValues.All(y => !x.PagePropertyHasValue(y)))
            .Select(x => new PageEntity(x))
            .ToArray();

        return new(pages);
    }

    [Action("Search single page by text property", Description = "Search a database for a single page that matches on a text property")]
    public async Task<PageEntity> SearchSinglePageInDatabase(
    [ActionParameter] StringPropertyWithValueRequest input)
    {
        var endpoint = $"{ApiEndpoints.Databases}/{input.DatabaseId}/query";
        var request = new NotionRequest(endpoint, Method.Post, Creds);

        var response = await Client.PaginateWithBody<PageResponse>(request);
        foreach (var page in response)
        {
            var property = page.FindPropertyById(input.PropertyId);
            var value = property?.GetStringValue();
            if (value == input.Value)
                return new PageEntity(page);
        }

        return new PageEntity(new PageResponse { });
    }

    [Action("Create database", Description = "Create a new database")]
    public async Task<DatabaseEntity> CreateDatabase([ActionParameter] CreateDatabaseInput input)
    {
        input.Properties ??= new List<PropertyRequest>();
        if (input.Properties.All(x => x.Type != "title"))
        {
            var mandatoryProperties = new List<PropertyRequest>()
            {
                new()
                {
                    Name = "Name",
                    Type = "title"
                }
            };
            input.Properties = input.Properties.Concat(mandatoryProperties);
        }

        var request = new NotionRequest(ApiEndpoints.Databases, Method.Post, Creds)
            .WithJsonBody(new CreateDatabaseRequest(input), JsonConfig.Settings);

        var response = await Client.ExecuteWithErrorHandling<DatabaseResponse>(request);
        return new(response);
    }

    [Action("Get database", Description = "Get details of a specific database")]
    public async Task<DatabaseEntity> GetDatabase([ActionParameter] DatabaseRequest input)
    {
        var endpoint = $"{ApiEndpoints.Databases}/{input.DatabaseId}";
        var request = new NotionRequest(endpoint, Method.Get, Creds);

        var response = await Client.ExecuteWithErrorHandling<DatabaseResponse>(request);
        return new(response);
    }
    
    public async Task<List<DatabaseJsonEntity>> SearchPagesInDatabaseAsJsonAsync(string databaseId)
    {
        var endpoint = $"{ApiEndpoints.Databases}/{databaseId}/query";
        var request = new NotionRequest(endpoint, Method.Post, Creds);

        var response = await Client.PaginateWithBody<DatabaseJsonEntity>(request);
        return response;
    }
    
    public async Task<JObject> GetDatabaseAsJson([ActionParameter] DatabaseRequest input)
    {
        var endpoint = $"{ApiEndpoints.Databases}/{input.DatabaseId}";
        var request = new NotionRequest(endpoint, Method.Get, Creds);

        var response = await Client.ExecuteWithErrorHandling<JObject>(request);
        return response;
    }
}