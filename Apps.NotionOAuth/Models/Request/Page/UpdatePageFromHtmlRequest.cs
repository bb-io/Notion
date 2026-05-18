using Apps.NotionOAuth.DataSourceHandlers;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.NotionOAuth.Models.Request.Page;

public class UpdatePageFromHtmlRequest : IUploadContentInput
{
    [Display("Content")] 
    public FileReference Content { get; set; } = null!;
    
    // Not mapped to anything
    string? IUploadContentInput.Locale { get; set; }
    
    [Display("Page ID"), DataSource(typeof(PageDataHandler))]
    public string? ContentId { get; set; }
}