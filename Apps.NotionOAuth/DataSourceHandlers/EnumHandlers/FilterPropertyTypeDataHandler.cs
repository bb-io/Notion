using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.NotionOAuth.DataSourceHandlers.EnumHandlers;

public class FilterPropertyTypeDataHandler : IStaticDataSourceHandler
{
    public Dictionary<string, string> GetData()
    {
        return new()
        {
            {"date", "Date"},
            {"files", "Files"},
            {"multi_select", "Multi-select"},
            {"number", "Number"},
            {"people", "People"},
            {"relation", "Relation"},
            {"rich_text", "Rich text"},
            {"rollup", "Rollup"},
            {"select", "Select"},
            {"status", "Status"},
        };
    }
}