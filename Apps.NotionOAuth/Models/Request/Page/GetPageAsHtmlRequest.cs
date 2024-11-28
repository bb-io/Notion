using Blackbird.Applications.Sdk.Common;

namespace Apps.NotionOAuth.Models.Request.Page;

public class GetPageAsHtmlRequest
{
    [Display("Include child pages")]
    public bool? IncludeChildPages { get; set; }
}