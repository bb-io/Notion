using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.DataSourceHandlers.PageProperties.Base;
using Apps.NotionOAuth.Models.Request.Page.Properties.Getter;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.NotionOAuth.DataSourceHandlers.PageProperties.Getters;

public class StringPagePropertiesDataHandler(InvocationContext invocationContext, [ActionParameter] PageStringPropertyRequest input)
    : PagePropertiesDataHandler(invocationContext, input.DatabaseId, input.PageId)
{
    protected override string[] Types =>
    [
        DatabasePropertyTypes.Title,
        DatabasePropertyTypes.Email,
        DatabasePropertyTypes.PhoneNumber,
        DatabasePropertyTypes.Status,
        DatabasePropertyTypes.CreatedBy,
        DatabasePropertyTypes.LastEditedBy,
        DatabasePropertyTypes.Select,
        DatabasePropertyTypes.Url,
        DatabasePropertyTypes.RichText,
        DatabasePropertyTypes.Relation
    ];
}