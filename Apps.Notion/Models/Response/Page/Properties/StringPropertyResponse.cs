using Blackbird.Applications.Sdk.Common;

namespace Apps.Notion.Models.Response.Page.Properties;

public class StringPropertyResponse
{
    [Display("Property value")]
    public string PropertyValue { get; set; }
}