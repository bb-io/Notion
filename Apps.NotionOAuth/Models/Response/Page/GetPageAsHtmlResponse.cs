using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.NotionOAuth.Models.Response.Page;

public class GetPageAsHtmlResponse(FileReference file) : IDownloadContentOutput
{
    [Display("Content")]
    public FileReference Content { get; set; } = file;
};