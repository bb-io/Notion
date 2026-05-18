using Apps.NotionOAuth.DataSourceHandlers;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.NotionOAuth.Models.Request.Page;

public class GetPageAsHtmlRequest : IDownloadContentInput
{
    [Display("Page ID"), DataSource(typeof(PageDataHandler))]
    public string ContentId { get; set; } = string.Empty;
    
    [Display("Include child pages")]
    public bool? IncludeChildPages { get; set; }
    
    [Display("Include child databases")]
    public bool? IncludeChildDatabases { get; set; }
    
    [Display("Include database text properties")] 
    public bool? IncludePageProperties { get; set; }
}