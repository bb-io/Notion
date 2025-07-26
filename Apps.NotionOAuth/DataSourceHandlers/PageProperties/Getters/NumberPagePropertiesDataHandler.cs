using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.DataSourceHandlers.PageProperties.Base;
using Apps.NotionOAuth.Models.Request.Page.Properties.Getter;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.NotionOAuth.DataSourceHandlers.PageProperties.Getters;

public class NumberPagePropertiesDataHandler(InvocationContext invocationContext, [ActionParameter] PageNumberPropertyRequest input)
    : PagePropertiesDataHandler(invocationContext, input.DatabaseId)
{
    protected override string[] Types =>
    [
        DatabasePropertyTypes.Number,
        DatabasePropertyTypes.UniqueId
    ];
}