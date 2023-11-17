using Apps.Notion.DataSourceHandlers.PageProperties.Base;
using Apps.Notion.Models.Request.Page.Properties.Getter;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Notion.DataSourceHandlers.PageProperties;

public class FilesPagePropertiesDataHandler : PagePropertiesDataHandler
{
    protected override string[] Types => new[] { "files" };

    public FilesPagePropertiesDataHandler(InvocationContext invocationContext,
        [ActionParameter] PageFilesPropertyRequest input) : base(invocationContext, input.DatabaseId)
    {
    }
}