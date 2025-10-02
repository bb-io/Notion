using Apps.NotionOAuth.Constants;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.NotionOAuth.DataSourceHandlers.EnumHandlers;

public class FilterPropertyTypeDataHandler : IStaticDataSourceItemHandler
{
    private static Dictionary<string, string> Data => new()
    {
        { DatabasePropertyTypes.Date, "Date" },
        { DatabasePropertyTypes.Files, "Files" },
        { DatabasePropertyTypes.MultiSelect, "Multi-select" },
        { DatabasePropertyTypes.Number, "Number" },
        { DatabasePropertyTypes.People, "People" },
        { DatabasePropertyTypes.Relation, "Relation" },
        { DatabasePropertyTypes.RichText, "Rich text" },
        { DatabasePropertyTypes.Rollup, "Rollup" },
        { DatabasePropertyTypes.Select, "Select" },
        { DatabasePropertyTypes.Status, "Status" },
    };

    public IEnumerable<DataSourceItem> GetData()
    {
        return Data.Select(x => new DataSourceItem(x.Key, x.Value));
    }
}