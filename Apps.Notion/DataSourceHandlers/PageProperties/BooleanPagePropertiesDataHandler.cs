using Apps.Notion.DataSourceHandlers.PageProperties.Base;
using Apps.Notion.Models.Request.Page.Properties.Getter;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Notion.DataSourceHandlers.PageProperties;

public class BooleanPagePropertiesDataHandler : PagePropertiesDataHandler
{
    protected override string[] Types => new[] { "checkbox" };

    public BooleanPagePropertiesDataHandler(InvocationContext invocationContext, PageBooleanPropertyRequest input) : base(
        invocationContext, input.PageId)
    {
    }
}