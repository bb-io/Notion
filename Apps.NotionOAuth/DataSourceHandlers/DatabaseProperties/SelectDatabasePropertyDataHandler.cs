using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.DataSourceHandlers.DatabaseProperties.Base;
using Apps.NotionOAuth.Models.Request.DataBase;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json.Linq;

namespace Apps.NotionOAuth.DataSourceHandlers.DatabaseProperties;

public class SelectDatabasePropertyDataHandler(InvocationContext invocationContext, [ActionParameter] SearchPagesInDatabaseRequest input)
    : DatabasePropertiesDataHandler(invocationContext, input.DatabaseId)
{
    protected override Dictionary<string, string> GetAppropriateProperties(Dictionary<string, JObject> properties)
    {
        return properties
            .Where(x => x.Value["type"]!.ToString() == DatabasePropertyTypes.Select)
            .SelectMany(x => 
                x.Value["select"]!["options"]!.Select(y => 
                    new KeyValuePair<string, string>($"{x.Value["id"]};{y["name"]}", $"{x.Key}={y["name"]}")))
            .ToDictionary(x => x.Key, x => x.Value);
    }
}