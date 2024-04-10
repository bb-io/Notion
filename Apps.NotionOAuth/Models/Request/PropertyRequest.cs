using Apps.NotionOAuth.DataSourceHandlers.EnumHandlers;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.NotionOAuth.Models.Request;

public class PropertyRequest
{
    public string Name { get; set; }
    
    [StaticDataSource(typeof(PropertyTypeDataHandler))]
    public string Type { get; set; }
}