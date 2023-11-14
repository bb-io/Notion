using Blackbird.Applications.Sdk.Common;

namespace Apps.Notion.Models.Response.Page.Properties;

public class MultipleStringPropertyResponse
{
    [Display("Property value")] public IEnumerable<string> PropertyValue { get; set; }
}