using Apps.Notion.DataSourceHandlers.PageProperties.Base;
using Apps.Notion.Models.Request.Page.Properties.Getter;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Notion.DataSourceHandlers.PageProperties.Getters;

public class BooleanPagePropertiesDataHandler : PagePropertiesDataHandler
{
    protected override string[] Types => new[] { "checkbox" };

    public BooleanPagePropertiesDataHandler(InvocationContext invocationContext,
        [ActionParameter] PageBooleanPropertyRequest input) : base(invocationContext, input.DatabaseId)
    {
    }
}