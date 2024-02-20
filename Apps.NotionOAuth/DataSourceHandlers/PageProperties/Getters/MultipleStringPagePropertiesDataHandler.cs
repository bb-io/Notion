using Apps.NotionOAuth.DataSourceHandlers.PageProperties.Base;
using Apps.NotionOAuth.Models.Request.Page.Properties.Getter;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.NotionOAuth.DataSourceHandlers.PageProperties.Getters;

public class MultipleStringPagePropertiesDataHandler : PagePropertiesDataHandler
{
    protected override string[] Types => new[] { "multi_select", "relation", "people" };

    public MultipleStringPagePropertiesDataHandler(InvocationContext invocationContext,
        [ActionParameter] PageMultipleStringPropertyRequest input) : base(invocationContext, input.DatabaseId)
    {
    }
}