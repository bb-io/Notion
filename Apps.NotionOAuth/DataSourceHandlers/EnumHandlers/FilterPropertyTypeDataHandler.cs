using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.NotionOAuth.DataSourceHandlers.EnumHandlers;

public class FilterPropertyTypeDataHandler : IStaticDataSourceItemHandler
{
    private static Dictionary<string, string> Data => new()
    {
        { "date", "Date" },
        { "files", "Files" },
        { "multi_select", "Multi-select" },
        { "number", "Number" },
        { "people", "People" },
        { "relation", "Relation" },
        { "rich_text", "Rich text" },
        { "rollup", "Rollup" },
        { "select", "Select" },
        { "status", "Status" },
    };

    public IEnumerable<DataSourceItem> GetData()
    {
        return Data.Select(x => new DataSourceItem(x.Key, x.Value));
    }
}