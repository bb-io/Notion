using Apps.NotionOAuth.Models.Request.Page.Properties.Getter;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.NotionOAuth.Models.Request.Page.Properties.Setter;

public class SetPageFilesPropertyRequest : PageFilesPropertyRequest
{
    public IEnumerable<FileReference> Values { get; set; }
}