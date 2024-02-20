using Apps.NotionOAuth.DataSourceHandlers.PageProperties.Base;
using Apps.NotionOAuth.Models.Request.Page.Properties.Getter;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.NotionOAuth.DataSourceHandlers.PageProperties.Getters;

public class NumberPagePropertiesDataHandler : PagePropertiesDataHandler
{
    protected override string[] Types => new[] { "number", "unique_id" };

    public NumberPagePropertiesDataHandler(InvocationContext invocationContext,
        [ActionParameter] PageNumberPropertyRequest input) : base(invocationContext, input.DatabaseId)
    {
    }
}