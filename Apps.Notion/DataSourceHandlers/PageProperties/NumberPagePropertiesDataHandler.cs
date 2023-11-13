using Apps.Notion.DataSourceHandlers.PageProperties.Base;
using Apps.Notion.Models.Request.Page.Properties.Getter;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Notion.DataSourceHandlers.PageProperties;

public class NumberPagePropertiesDataHandler : PagePropertiesDataHandler
{
    protected override string[] Types => new[] { "number", "unique_id" };

    public NumberPagePropertiesDataHandler(InvocationContext invocationContext,
        [ActionParameter] PageNumberPropertyRequest input) : base(invocationContext, input.PageId)
    {
    }
}