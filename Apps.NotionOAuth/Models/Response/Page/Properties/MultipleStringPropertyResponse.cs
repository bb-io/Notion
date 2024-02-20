using Blackbird.Applications.Sdk.Common;

namespace Apps.NotionOAuth.Models.Response.Page.Properties;

public class MultipleStringPropertyResponse
{
    [Display("Property value")] public IEnumerable<string> PropertyValue { get; set; }
}