using Apps.NotionOAuth.DataSourceHandlers.PageProperties.Base;
using Apps.NotionOAuth.Models.Request.DataBase.Properties;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.NotionOAuth.DataSourceHandlers.PageProperties.Getters;

public class StringPagePropertiesDataHandler : PagePropertiesDataHandler
{
    protected override string[] Types => new[]
        { "title", "email", "phone_number", "status", "created_by", "last_edited_by", "select", "url", "rich_text", "relation" };

    public StringPagePropertiesDataHandler(InvocationContext invocationContext,
        [ActionParameter] StringPropertyRequest input) : base(invocationContext, input.DatabaseId)
    {
    }
}