using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Apps.NotionOAuth.DataSourceHandlers.EnumHandlers;

namespace Apps.NotionOAuth.Models.Request.View;

public class CreateViewRequest
{
    [Display("View name")]
    public string Name { get; set; } = string.Empty;

    [Display("View type"), StaticDataSource(typeof(ViewTypeDataHandler))]
    public string Type { get; set; } = string.Empty;
}
