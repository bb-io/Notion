using Blackbird.Applications.Sdk.Common;

namespace Apps.NotionOAuth.Models.Response.Page.Properties;

public class StringPropertyResponse
{
    [Display("Property value")]
    public string PropertyValue { get; set; }
}