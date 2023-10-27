using Blackbird.Applications.Sdk.Common;

namespace Apps.Notion.Models.Request.DataBase;

public class DatabaseRequest
{
    //todo: add dynamic input
    [Display("Database")]
    public string DatabaseId { get; set; }
}