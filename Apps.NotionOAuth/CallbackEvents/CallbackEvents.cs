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
        if (string.IsNullOrWhiteSpace(filterHeaderName) != string.IsNullOrWhiteSpace(filterHeaderExpectedPart))
        {
            return Task.FromResult(new WebhookResponse<ButtonClickedResponse>
            {
                ReceivedWebhookRequestType = WebhookRequestType.Preflight, // don't start flight
                HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
                Result = null
            });
        }

        if (string.IsNullOrWhiteSpace(filterHeaderName) == false
            && webhookRequest.Headers.TryGetValue(filterHeaderName, out var filterHeaderValue)
            && filterHeaderValue.Contains(filterHeaderExpectedPart!, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new WebhookResponse<ButtonClickedResponse>
            {
                ReceivedWebhookRequestType = WebhookRequestType.Preflight, // don't start flight
                HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
                Result = null
            });
        }

        var body = JsonConvert.DeserializeObject<ButtonRequest>(webhookRequest.Body.ToString() ?? string.Empty)
            ?? throw new PluginApplicationException($"Can't deserialize button request body. Received: {webhookRequest.Body}");

        return Task.FromResult(new WebhookResponse<ButtonClickedResponse>
        {
            ReceivedWebhookRequestType = WebhookRequestType.Default, // start flight
            HttpResponseMessage = new HttpResponseMessage(statusCode: HttpStatusCode.OK),
            Result = new ButtonClickedResponse()
            {
                PageId = body.Data.PageId,
                ParentType = body.Parent.Type,
                ParentId = body.Parent.GetParentId(),
            }
        });
    }
}
