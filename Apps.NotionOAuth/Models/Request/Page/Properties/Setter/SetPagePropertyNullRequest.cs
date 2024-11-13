using Apps.NotionOAuth.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common;
using Apps.NotionOAuth.DataSourceHandlers.PageProperties.Base;
using Apps.NotionOAuth.DataSourceHandlers.PageProperties.Setters;

namespace Apps.NotionOAuth.Models.Request.Page.Properties.Setter
{
    public class SetPagePropertyNullRequest
    {

        [Display("Database ID")]
        [DataSource(typeof(DatabaseDataHandler))]
        public string DatabaseId { get; set; }

        [Display("Page ID")]
        [DataSource(typeof(PageDataHandler))]
        public string PageId { get; set; }

        [Display("Property ID")]
        [DataSource(typeof(SetNulllablePagePropertyDataHandler))]
        public string PropertyId { get; set; }
    }
}
