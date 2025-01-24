using Blackbird.Applications.Sdk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.NotionOAuth.CallbackEvents.Models.Responses;
public class ButtonClickedResponse
{
    [Display("Page ID")]
    public string PageId { get; set; }
}
