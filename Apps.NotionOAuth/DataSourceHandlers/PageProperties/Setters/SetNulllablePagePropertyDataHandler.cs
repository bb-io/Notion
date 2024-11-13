using Apps.NotionOAuth.DataSourceHandlers.PageProperties.Base;
using Apps.NotionOAuth.Models.Request.Page.Properties.Setter;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.NotionOAuth.DataSourceHandlers.PageProperties.Setters;

public class SetNulllablePagePropertyDataHandler : PagePropertiesDataHandler
{
    protected override string[] Types => new[] { "phone_number", "email", "url", "number", "status", "select", "checkbox", "multi_select", "rich_text", "files", "relation", "people" };

    public SetNulllablePagePropertyDataHandler(InvocationContext invocationContext,
        [ActionParameter] SetPageDatePropertyRequest input) : base(invocationContext, input.DatabaseId)
    {
    }
}