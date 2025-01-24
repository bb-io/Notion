using Apps.NotionOAuth.CallbackEvents.Models.Dto;
using Apps.NotionOAuth.CallbackEvents.Models.Responses;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Apps.NotionOAuth.CallbackEvents;

[WebhookList]
public class CallbackEvents
{
    [Webhook("On button clicked", Description = "Triggered when you click a button on a Notion page. See")]
    public Task<WebhookResponse<ButtonClickedResponse>> OrderDeleted(WebhookRequest webhookRequest)
    {
        var body = JsonConvert.DeserializeObject<ButtonRequest>(webhookRequest.Body.ToString());
        return Task.FromResult(new WebhookResponse<ButtonClickedResponse>
        {
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new ButtonClickedResponse()
            {
                PageId = body?.Data.Id,
            }
        });
    }
}
