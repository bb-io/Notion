using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.NotionOAuth.Models.Request;

public class FileRequest
{
    [Display("HTML file")]
    public FileReference File { get; set; }
}