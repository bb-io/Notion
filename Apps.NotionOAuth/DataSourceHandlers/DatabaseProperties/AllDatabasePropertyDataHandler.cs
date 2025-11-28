using Apps.NotionOAuth.DataSourceHandlers.DatabaseProperties.Base;
using Apps.NotionOAuth.Models.Request.DataBase;
using Apps.NotionOAuth.Models.Request.DataSource;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json.Linq;

namespace Apps.NotionOAuth.DataSourceHandlers.DatabaseProperties;

public class AllDatabasePropertyDataHandler(InvocationContext invocationContext, [ActionParameter] DatabaseRequest databaseRequest, [ActionParameter] DataSourceRequest dataSourceRequest)
    : DatabasePropertiesDataHandler(invocationContext, databaseRequest.DatabaseId, dataSourceRequest.DataSourceId)
{
    protected override Dictionary<string, string> GetAppropriateProperties(Dictionary<string, JObject> properties)
    {
        return properties
            .ToDictionary(x => x.Value["id"]?.ToString() ?? string.Empty, x => x.Key);
    }
}