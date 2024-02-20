using Blackbird.Applications.Sdk.Common;

namespace Apps.NotionOAuth.Models.Response.Page.Properties;

public class DatePropertyResponse
{
    [Display("Property value")] public DateTime PropertyValue { get; set; }
}