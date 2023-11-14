using Blackbird.Applications.Sdk.Common;

namespace Apps.Notion.Models.Request;

public class ListRequest
{
    [Display("Edited since")]
    public DateTime? EditedSince { get; set; }
}