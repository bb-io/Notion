using Blackbird.Applications.Sdk.Common;

namespace Apps.Notion.Models.Response.Page.Properties;

public class BooleanPropertyResponse
{
    [Display("Property value")]
    public bool PropertyValue { get; set; }
}