using Blackbird.Applications.Sdk.Common;

namespace Apps.Notion.Models.Request.Page;

public class PageRequest
{
    // todo: add dynamic inputs
    [Display("Page")]
    public string PageId { get; set; }
}