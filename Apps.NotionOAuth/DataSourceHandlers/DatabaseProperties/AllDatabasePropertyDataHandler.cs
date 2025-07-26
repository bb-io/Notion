using Apps.NotionOAuth.DataSourceHandlers.DatabaseProperties.Base;
using Apps.NotionOAuth.Models.Request.DataBase;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json.Linq;

namespace Apps.NotionOAuth.DataSourceHandlers.DatabaseProperties;

public class AllDatabasePropertyDataHandler(InvocationContext invocationContext, [ActionParameter] SearchPagesInDatabaseRequest input)
    : DatabasePropertiesDataHandler(invocationContext, input.DatabaseId)
{
    protected override Dictionary<string, string> GetAppropriateProperties(Dictionary<string, JObject> properties)
    {
        return properties
            .ToDictionary(x => x.Value["id"]?.ToString() ?? string.Empty, x => x.Key);
    }
}