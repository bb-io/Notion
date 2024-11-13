using Apps.NotionOAuth.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common;
using Apps.NotionOAuth.DataSourceHandlers.PageProperties.Base;

namespace Apps.NotionOAuth.Models.Request.Page.Properties.Setter
{
    public class SetPagePropertyNullRequest
    {

        [Display("Database")]
        [DataSource(typeof(DatabaseDataHandler))]
        public string DatabaseId { get; set; }

        [Display("Page")]
        [DataSource(typeof(PageDataHandler))]
        public string PageId { get; set; }

        [Display("Property")]
        [DataSource(typeof(PagePropertiesDataHandler))]
        public string PropertyId { get; set; }
    }
}
