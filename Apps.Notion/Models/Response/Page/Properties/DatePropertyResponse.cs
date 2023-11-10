using Blackbird.Applications.Sdk.Common;

namespace Apps.Notion.Models.Response.Page.Properties;

public class DatePropertyResponse
{
    [Display("Property value")] public DateTime PropertyValue { get; set; }
}