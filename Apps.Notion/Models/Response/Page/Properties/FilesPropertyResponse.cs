using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Notion.Models.Response.Page.Properties;

public class FilesPropertyResponse
{
    [Display("Property value")]
    public IEnumerable<FileReference> PropertyValue { get; set; }
}