using Apps.Notion.DataSourceHandlers.PageProperties.Base;
using Apps.Notion.Models.Request.Page.Properties.Getter;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Notion.DataSourceHandlers.PageProperties;

public class MultipleStringPagePropertiesDataHandler : PagePropertiesDataHandler
{
    protected override string[] Types => new[] { "multi_select", "relation", "people" };

    public MultipleStringPagePropertiesDataHandler(InvocationContext invocationContext,
        [ActionParameter] PageMultipleStringPropertyRequest input) : base(
        invocationContext, input.PageId)
    {
    }
}