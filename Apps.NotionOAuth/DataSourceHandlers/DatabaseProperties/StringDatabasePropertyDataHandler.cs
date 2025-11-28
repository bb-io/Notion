using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.DataSourceHandlers.DatabaseProperties.Base;
using Apps.NotionOAuth.Models.Request.DataBase;
using Apps.NotionOAuth.Models.Request.DataSource;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json.Linq;

namespace Apps.NotionOAuth.DataSourceHandlers.DatabaseProperties;

public class StringDatabasePropertyDataHandler(InvocationContext invocationContext, [ActionParameter] DatabaseRequest databaseRequest, [ActionParameter] DataSourceRequest dataSourceRequest)
    : DatabasePropertiesDataHandler(invocationContext, databaseRequest.DatabaseId, dataSourceRequest.DataSourceId)
{
    private string[] Types =>
    [
        DatabasePropertyTypes.Title,
        DatabasePropertyTypes.Email,
        DatabasePropertyTypes.PhoneNumber,
        DatabasePropertyTypes.Status,
        DatabasePropertyTypes.CreatedBy,
        DatabasePropertyTypes.LastEditedBy,
        DatabasePropertyTypes.Select,
        DatabasePropertyTypes.Url,
        DatabasePropertyTypes.RichText,
        DatabasePropertyTypes.Relation
    ];

    protected override Dictionary<string, string> GetAppropriateProperties(Dictionary<string, JObject> properties)
    {
        return properties
            .Where(prop => Types.Contains(prop.Value["type"]?.ToString()))
            .Where(prop => prop.Value["id"]?.ToString() != null)
            .ToDictionary(prop => prop.Value["id"]?.ToString() ?? string.Empty, prop => prop.Key);
    }
}