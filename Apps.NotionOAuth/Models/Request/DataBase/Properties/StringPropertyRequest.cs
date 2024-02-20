using Apps.NotionOAuth.DataSourceHandlers.PageProperties.Getters;
using Apps.NotionOAuth.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blackbird.Applications.Sdk.Common;

namespace Apps.NotionOAuth.Models.Request.DataBase.Properties
{
    public class StringPropertyRequest
    {
        [Display("Database")]
        [DataSource(typeof(DatabaseDataHandler))]
        public string DatabaseId { get; set; }

        [Display("Property")]
        [DataSource(typeof(StringPagePropertiesDataHandler))]
        public string PropertyId { get; set; }
    }
}
