using Apps.Notion.DataSourceHandlers.PageProperties.Base;
using Apps.Notion.Models.Request.Page.Properties;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Notion.DataSourceHandlers.PageProperties;

public class StringPagePropertiesDataHandler : PagePropertiesDataHandler
{
    protected override string[] Types => new[]
        { "title", "email", "phone_number", "status", "created_by", "last_edited_by", "select", "url", "rich_text" };

    public StringPagePropertiesDataHandler(InvocationContext invocationContext,
        [ActionParameter] PageStringPropertyRequest input) : base(invocationContext, input.PageId)
    {
    }
}