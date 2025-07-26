using Apps.NotionOAuth.DataSourceHandlers;
using Apps.NotionOAuth.DataSourceHandlers.PageProperties.Getters;
using Apps.NotionOAuth.Models.Request.Page.Properties.Getter;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.NotionOAuth.Models.Request.Page.Properties.Setter;

public class SetPageRelationPropertyRequest
{
    [Display("Database")]
    [DataSource(typeof(DatabaseDataHandler))]
    public string DatabaseId { get; set; } = string.Empty;

    [Display("Page")]
    [DataSource(typeof(PageDataHandler))]
    public string PageId { get; set; } = string.Empty;

    [Display("Property")]
    [DataSource(typeof(RelationPagePropertiesDataHandler))]
    public string PropertyId { get; set; } = string.Empty;

    [Display("Related page IDs")]
    [DataSource(typeof(PageDataHandler))]
    public IEnumerable<string> RelatedPageIds { get; set; } = [];
}