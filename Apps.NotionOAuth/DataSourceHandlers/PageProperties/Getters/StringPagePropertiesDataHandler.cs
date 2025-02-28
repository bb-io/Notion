using Apps.NotionOAuth.DataSourceHandlers.PageProperties.Base;
using Apps.NotionOAuth.Models.Request.DataBase.Properties;
using Apps.NotionOAuth.Models.Request.Page.Properties.Getter;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.NotionOAuth.DataSourceHandlers.PageProperties.Getters;

public class StringPagePropertiesDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] PageStringPropertyRequest input)
    : PagePropertiesDataHandler(invocationContext, input.DatabaseId, input.PageId)
{
    protected override string[] Types => ["title", "email", "phone_number", "status", "created_by", "last_edited_by", "select", "url", "rich_text", "relation"];
}