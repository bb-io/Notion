using Blackbird.Applications.Sdk.Common;

namespace Apps.NotionOAuth.CallbackEvents.Models.Responses;
public class ButtonClickedResponse
{
    [Display("Page ID")]
    public string PageId { get; set; } = string.Empty;

    [Display("Parent Type")]
    public string ParentType { get; set; } = string.Empty;

    [Display("Parent ID")]
    public string ParentId { get; set; } = string.Empty;
}
