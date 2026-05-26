using Blackbird.Applications.Sdk.Common;

namespace Apps.NotionOAuth.Models.Response.Page.Properties;

public class FormulaPropertyResponse
{
    [Display("Result type")]
    public string ResultType { get; set; } = string.Empty;

    [Display("Property value")]
    public string PropertyValue { get; set; } = string.Empty;

    [Display("String value")]
    public string? StringValue { get; set; }

    [Display("Number value")]
    public decimal? NumberValue { get; set; }

    [Display("Boolean value")]
    public bool? BooleanValue { get; set; }

    [Display("Date value")]
    public DateTime? DateValue { get; set; }
}
