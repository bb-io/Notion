using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.DataSourceHandlers.PageProperties.Base;
using Apps.NotionOAuth.Models.Request.Page.Properties.Setter;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.NotionOAuth.DataSourceHandlers.PageProperties.Setters;

public class SetDatePagePropertyDataHandler(InvocationContext invocationContext, [ActionParameter] SetPageDatePropertyRequest input)
    : PagePropertiesDataHandler(invocationContext, input.DatabaseId)
{
    protected override string[] Types => 
        [
            DatabasePropertyTypes.Date
        ];
}