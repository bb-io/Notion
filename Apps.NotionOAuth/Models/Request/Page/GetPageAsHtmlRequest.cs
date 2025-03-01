﻿using Blackbird.Applications.Sdk.Common;

namespace Apps.NotionOAuth.Models.Request.Page;

public class GetPageAsHtmlRequest
{
    [Display("Include child pages")]
    public bool? IncludeChildPages { get; set; }
    
    [Display("Include child databases")]
    public bool? IncludeChildDatabases { get; set; }
    
    [Display("Include database text properties")] 
    public bool? IncludePageProperties { get; set; }
}