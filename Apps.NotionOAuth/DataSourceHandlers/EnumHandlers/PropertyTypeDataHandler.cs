using Apps.NotionOAuth.Constants;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.NotionOAuth.DataSourceHandlers.EnumHandlers;

public class PropertyTypeDataHandler : IStaticDataSourceItemHandler
{
    private static Dictionary<string, string> Data => new()
    {
        { DatabasePropertyTypes.Title, "Title" },
        { DatabasePropertyTypes.RichText, "Rich text" },
        { DatabasePropertyTypes.Number, "Number" },
        { DatabasePropertyTypes.Select, "Select" },
        { DatabasePropertyTypes.MultiSelect, "Multi-Select" },
        { DatabasePropertyTypes.Date, "Date" },
        { DatabasePropertyTypes.People, "People" },
        { DatabasePropertyTypes.Files, "Files" },
        { DatabasePropertyTypes.Checkbox, "Checkbox" },
        { DatabasePropertyTypes.Url, "URL" },
        { DatabasePropertyTypes.Email, "Email" },
        { DatabasePropertyTypes.PhoneNumber, "Phone number" },
        { DatabasePropertyTypes.Formula, "Formula" },
        { DatabasePropertyTypes.Relation, "Relation" },
        { DatabasePropertyTypes.Rollup, "Rollup" },
        { DatabasePropertyTypes.CreatedTime, "Created time" },
        { DatabasePropertyTypes.CreatedBy, "Created by" },
        { DatabasePropertyTypes.LastEditedTime, "Last edited time" },
        { DatabasePropertyTypes.LastEditedBy, "Last edited by" }
    };
    
    public IEnumerable<DataSourceItem> GetData()
    {
        return Data.Select(x => new DataSourceItem(x.Key, x.Value));
    }
}