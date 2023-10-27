using Blackbird.Applications.Sdk.Common;

namespace Apps.Notion.Models.Response.DataBase;

public class DatabaseParentResponse
{
    public string Type { get; set; }
    
    [Display("Page ID")]
    public string PageId { get; set; }
}