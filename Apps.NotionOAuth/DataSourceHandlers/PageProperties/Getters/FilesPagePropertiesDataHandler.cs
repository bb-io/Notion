using Apps.NotionOAuth.DataSourceHandlers.PageProperties.Base;
using Apps.NotionOAuth.Models.Request.Page.Properties.Getter;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.NotionOAuth.DataSourceHandlers.PageProperties.Getters;

public class FilesPagePropertiesDataHandler : PagePropertiesDataHandler
{
    protected override string[] Types => new[] { "files" };

    public FilesPagePropertiesDataHandler(InvocationContext invocationContext,
        [ActionParameter] PageFilesPropertyRequest input) : base(invocationContext, input.DatabaseId)
    {
    }
}