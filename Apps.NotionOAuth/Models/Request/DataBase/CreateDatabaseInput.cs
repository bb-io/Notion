using Apps.NotionOAuth.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.NotionOAuth.Models.Request.DataBase;

public class CreateDatabaseInput
{
    [Display("Parent page ID")]
    [DataSource(typeof(PageDataHandler))]
    public string PageId { get; set; }
    
    public string Title { get; set; }
    
    public IEnumerable<PropertyRequest>? Properties { get; set; }
}