using Apps.NotionOAuth.DataSourceHandlers.DatabaseProperties.Base;
using Apps.NotionOAuth.Models.Request.DataBase;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json.Linq;

namespace Apps.NotionOAuth.DataSourceHandlers.DatabaseProperties;

public class CheckboxDatabasePropertyDataHandler : DatabasePropertiesDataHandler
{
    public CheckboxDatabasePropertyDataHandler(InvocationContext invocationContext, [ActionParameter] SearchPagesInDatabaseRequest input)
        : base(invocationContext, input.DatabaseId)
    {
    }

    protected override Dictionary<string, string> GetAppropriateProperties(Dictionary<string, JObject> properties)
    {
        return properties
            .Where(x => x.Value["type"]!.ToString() == "checkbox")
            .SelectMany(x => new KeyValuePair<string, string>[]
                { new($"{x.Value["id"]};False", $"{x.Key}=False"), new($"{x.Value["id"]};True", $"{x.Key}=True") })
            .ToDictionary(x => x.Key, x => x.Value);
    }
}