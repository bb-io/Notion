using Apps.NotionOAuth.CallbackEvents.Models.Dto;
using Apps.NotionOAuth.CallbackEvents.Models.Responses;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Newtonsoft.Json;
using System.Net;

namespace Apps.NotionOAuth.CallbackEvents;

[WebhookList]
public class CallbackEvents
{
    [Webhook("On button clicked", Description = "Triggered when you click a button on a Notion page. See")]
    public Task<WebhookResponse<ButtonClickedResponse>> OrderDeleted(
        WebhookRequest webhookRequest,
        [WebhookParameter, Display("Custom header key")] string? filterHeaderName,
        [WebhookParameter, Display("Custom header contains")] string? filterHeaderExpectedPart)
    {
        var hasHeaderName = !string.IsNullOrWhiteSpace(filterHeaderName);
        var hasExpectedPart = !string.IsNullOrWhiteSpace(filterHeaderExpectedPart);

        if (hasHeaderName != hasExpectedPart)
        {
            return Task.FromResult(new WebhookResponse<ButtonClickedResponse>
            {
                ReceivedWebhookRequestType = WebhookRequestType.Preflight,
                HttpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK),
                Result = null
            });
        }

        if (hasHeaderName && hasExpectedPart)
        {
            var matchedHeader = webhookRequest.Headers
                .FirstOrDefault(x => string.Equals(x.Key, filterHeaderName, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(matchedHeader.Key) ||
                string.IsNullOrWhiteSpace(matchedHeader.Value) ||
                !matchedHeader.Value.Contains(filterHeaderExpectedPart!, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new WebhookResponse<ButtonClickedResponse>
                {
                    ReceivedWebhookRequestType = WebhookRequestType.Preflight,
                    HttpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK),
                    Result = null
                });
            }
        }

        var body = JsonConvert.DeserializeObject<ButtonRequest>(webhookRequest.Body.ToString() ?? string.Empty)
            ?? throw new PluginApplicationException($"Can't deserialize button request body. Received: {webhookRequest.Body}");

        return Task.FromResult(new WebhookResponse<ButtonClickedResponse>
        {
            ReceivedWebhookRequestType = WebhookRequestType.Default,
            HttpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK),
            Result = new ButtonClickedResponse
            {
                PageId = body.Data.PageId,
                ParentType = body.Data.Parent.Type,
                ParentId = body.Data.Parent.GetParentId(),
            }
        });
    }
}
