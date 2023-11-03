using Blackbird.Applications.Sdk.Common;
using File = Blackbird.Applications.Sdk.Common.Files.File;

namespace Apps.Notion.Models.Request;

public class FileRequest
{
    [Display("HTML file")]
    public File File { get; set; }
}