using Apps.NotionOAuth.DataSourceHandlers;
using Apps.NotionOAuth.DataSourceHandlers.PageProperties.Getters;
using Apps.NotionOAuth.Models.Request.DataBase.Properties;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.NotionOAuth.Models.Request.Page.Properties.Getter;

public class PageStringPropertyRequest : StringPropertyRequest
{
    [Display("Page")]
    [DataSource(typeof(PageDataHandler))]
    public string PageId { get; set; }
}