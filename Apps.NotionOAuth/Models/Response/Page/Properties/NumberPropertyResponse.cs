using Blackbird.Applications.Sdk.Common;

namespace Apps.NotionOAuth.Models.Response.Page.Properties;

public class NumberPropertyResponse
{
    [Display("Property value")]
    public decimal PropertyValue { get; set; }
}