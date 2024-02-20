using Apps.NotionOAuth.DataSourceHandlers.PageProperties.Base;
using Apps.NotionOAuth.Models.Request.Page.Properties.Setter;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.NotionOAuth.DataSourceHandlers.PageProperties.Setters;

public class SetDatePagePropertyDataHandler : PagePropertiesDataHandler
{
    protected override string[] Types => new[] { "date" };

    public SetDatePagePropertyDataHandler(InvocationContext invocationContext,
        [ActionParameter] SetPageDatePropertyRequest input) : base(invocationContext, input.DatabaseId)
    {
    }
}