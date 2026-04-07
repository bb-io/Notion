using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.NotionOAuth.DataSourceHandlers.EnumHandlers;

public class ViewTypeDataHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return
        [
            new DataSourceItem("table", "Table"),
            new DataSourceItem("board", "Board"),
            new DataSourceItem("list", "List"),
            new DataSourceItem("calendar", "Calendar"),
            new DataSourceItem("timeline", "Timeline"),
            new DataSourceItem("gallery", "Gallery"),
            new DataSourceItem("form", "Form"),
            new DataSourceItem("chart", "Chart"),
            new DataSourceItem("map", "Map"),
            new DataSourceItem("dashboard", "Dashboard"),
        ];
    }
}
