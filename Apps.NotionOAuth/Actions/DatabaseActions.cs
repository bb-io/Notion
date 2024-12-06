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
    [Action("List databases", Description = "List all databases")]
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

    [Action("Search pages in database", Description = "Search pages in database that match specific condition")]
    public async Task<ListPagesResponse> SearchPagesInDatabase(
        [ActionParameter] SearchPagesInDatabaseRequest input)
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
            .Where(x => input.CheckboxProperty is null || FilterCheckboxProperty(x, input.CheckboxProperty))
            .Where(x => input.SelectProperty is null || FilterSelectProperty(x, input.SelectProperty))
            .Where(x => input.PropertiesShouldHaveValue is null || input.PropertiesShouldHaveValue.All(y => PagePropertyHasValue(x, y)))
            .Where(x => input.PropertiesWithoutValues is null || input.PropertiesWithoutValues.All(y => !PagePropertyHasValue(x, y)))
            .Select(x => new PageEntity(x))
            .ToArray();

        return new(pages);
    }

    public async Task<List<DatabaseJsonEntity>> SearchPagesInDatabaseAsJsonAsync(string databaseId)
    {
        var endpoint = $"{ApiEndpoints.Databases}/{databaseId}/query";
        var request = new NotionRequest(endpoint, Method.Post, Creds);

        var response = await Client.PaginateWithBody<DatabaseJsonEntity>(request);
        return response;
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
    
    public async Task<JObject> GetDatabaseAsJson([ActionParameter] DatabaseRequest input)
    {
        var endpoint = $"{ApiEndpoints.Databases}/{input.DatabaseId}";
        var request = new NotionRequest(endpoint, Method.Get, Creds);

        var response = await Client.ExecuteWithErrorHandling<JObject>(request);
        return response;
    }

    #region Utils

    private bool FilterSelectProperty(PageResponse pageResponse, string inputSelectProperty)
    {
        var propertyData = inputSelectProperty.Split(';');

        var propertyId = propertyData[0];
        var propertyValue = propertyData[1];

        return pageResponse.Properties.Any(x =>
            x.Value["id"]!.ToString() == propertyId && x.Value.SelectToken("select.name")?.ToString() == propertyValue);
    }

    private bool FilterCheckboxProperty(PageResponse pageResponse, string inputCheckboxProperty)
    {
        var propertyData = inputCheckboxProperty.Split(';');

        var propertyId = propertyData[0];
        var propertyValue = propertyData[1];

        return pageResponse.Properties.Any(x =>
            x.Value["id"]!.ToString() == propertyId && x.Value["checkbox"]?.ToString() == propertyValue);
    }

    private bool PagePropertyHasValue(PageResponse page, string propertyId)
    {
        KeyValuePair<string, JObject>? propertyPair =
            page.Properties.FirstOrDefault(x => x.Value["id"].ToString() == propertyId);

        var property = propertyPair?.Value ?? throw new("No property found with the provided ID");
        var propertyType = property["type"].ToString();

        return propertyType switch
        {
            "formula" => property[propertyType][property[propertyType]["type"].ToString()].HasValues ||
                         (property[propertyType][property[propertyType]["type"].ToString()] as JValue)?.Value != null,
            "rollup" => property[propertyType][property[propertyType]["type"].ToString()].HasValues ||
                        (property[propertyType][property[propertyType]["type"].ToString()] as JValue)?.Value != null,
            _ => property[propertyType].HasValues || (property[propertyType] as JValue)?.Value != null,
        };
    }

    #endregion
}