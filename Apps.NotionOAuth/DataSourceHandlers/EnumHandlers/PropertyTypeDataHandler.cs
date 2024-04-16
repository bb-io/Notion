using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.NotionOAuth.DataSourceHandlers.EnumHandlers;

public class PropertyTypeDataHandler : IStaticDataSourceHandler
{
    public Dictionary<string, string> GetData() => new()
    {
        { "title", "Title" },
        { "rich_text", "Rich text" },
        { "number", "Number" },
        { "select", "Select" },
        { "multi_select", "Multi-Select" },
        { "date", "Date" },
        { "people", "People" },
        { "files", "Files" },
        { "checkbox", "Checkbox" },
        { "url", "URL" },
        { "email", "Email" },
        { "phone_number", "Phone number" },
        { "formula", "Formula" },
        { "relation", "Relation" },
        { "rollup", "Rollup" },
        { "created_time", "Created time" },
        { "created_by", "Created by" },
        { "last_edited_time", "Last edited time" },
        { "last_edited_by", "Last edited by" }
    };
}