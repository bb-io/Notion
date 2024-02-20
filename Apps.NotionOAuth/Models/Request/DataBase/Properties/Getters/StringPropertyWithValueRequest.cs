using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.NotionOAuth.Models.Request.DataBase.Properties.Getters
{
    public class StringPropertyWithValueRequest : StringPropertyRequest
    {
        public string Value { get; set; }
    }
}
