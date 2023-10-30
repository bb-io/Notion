using Apps.Notion.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Notion.Models.Request;

public class PropertyRequest
{
    public string Name { get; set; }
    
    [DataSource(typeof(PropertyTypeDataHandler))]
    public string Type { get; set; }
}