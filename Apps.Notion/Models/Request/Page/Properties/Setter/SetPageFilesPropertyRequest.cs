using Apps.Notion.Models.Request.Page.Properties.Getter;
using File = Blackbird.Applications.Sdk.Common.Files.File;

namespace Apps.Notion.Models.Request.Page.Properties.Setter;

public class SetPageFilesPropertyRequest : PageFilesPropertyRequest
{
    public IEnumerable<File> Values { get; set; }
}