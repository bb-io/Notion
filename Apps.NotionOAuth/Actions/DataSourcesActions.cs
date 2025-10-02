using Apps.NotionOAuth.Api;
using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Entities;
using Apps.NotionOAuth.Models.Request.DataSource;
using Apps.NotionOAuth.Models.Response.Page;
using Apps.NotionOAuth.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.NotionOAuth.Actions;

[ActionList]
public class DataSourcesActions(InvocationContext invocationContext) : NotionInvocable(invocationContext)
{
    [Action("Search pages in datasource", Description = "Search pages in a specific datasource")]
    public async Task<ListPagesResponse> SearchPagesInDatasource([ActionParameter] DataSourceRequest dataSourceRequest,
        [ActionParameter] SearchPagesInDataSourceRequest searchRequest)
    {
        var endpoint = $"{ApiEndpoints.DataSources}/{dataSourceRequest.DataSourceId}/query";
        var request = new NotionRequest(endpoint, Method.Post, Creds, ApiConstants.LatestApiVersion);
        Dictionary<string, object>? bodyDictionary = null;

        if(searchRequest.FilterProperty != null && searchRequest.FilterPropertyType != null)
        {
            if(searchRequest.FilterValue == null && searchRequest.FilterValueIsEmpty == null)
                throw new("'Filter value' or 'Filter value must be empty' must be provided");
            
            var filterValueDict = new Dictionary<string, object>();
            
            if (searchRequest.FilterValueIsEmpty != null)
            {
                filterValueDict["is_not_empty"] = !searchRequest.FilterValueIsEmpty;
            }

            if (searchRequest.FilterValue != null)
            {
                filterValueDict["equals"] = searchRequest.FilterValue;
            }
            
            bodyDictionary = new Dictionary<string, object>
            {
                ["filter"] = new Dictionary<string, object>
                {
                    ["property"] = searchRequest.FilterProperty,
                    [searchRequest.FilterPropertyType] = filterValueDict
                }
            };
        }

        var response = await Client.PaginateWithBody<PageResponse>(request, bodyDictionary);
        var pages = response
            .Where(x => x.LastEditedTime > (searchRequest.EditedSince ?? default))
            .Where(x => x.CreatedTime > (searchRequest.CreatedSince ?? default))
            .Where(x => searchRequest.CheckboxProperty is null || x.FilterCheckboxProperty(searchRequest.CheckboxProperty))
            .Where(x => searchRequest.SelectProperty is null || x.FilterSelectProperty(searchRequest.SelectProperty))
            .Where(x => searchRequest.PropertiesShouldHaveValue is null || searchRequest.PropertiesShouldHaveValue.All(x.PagePropertyHasValue))
            .Where(x => searchRequest.PropertiesWithoutValues is null || searchRequest.PropertiesWithoutValues.All(y => !x.PagePropertyHasValue(y)))
            .Select(x => new PageEntity(x))
            .ToArray();

        return new(pages);
    }
}