using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Entities;
using Apps.NotionOAuth.Models.Response.Page;
using Apps.NotionOAuth.PollingEvents.Models.Memory;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;

namespace Apps.NotionOAuth.PollingEvents;

[PollingEventList]
public class PollingList : NotionInvocable
{
    public PollingList(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    [PollingEvent("On pages created", "On new pages created")]
    public Task<PollingEventResponse<DateMemory, ListPagesResponse>> OnPagesCreated(
        PollingEventRequest<DateMemory> request)
        => HandlePagesPolling(request,
            x => x.CreatedTime > request.Memory?.LastInteractionDate);

    [PollingEvent("On pages updated", "On any pages updated")]
    public Task<PollingEventResponse<DateMemory, ListPagesResponse>> OnPagesUpdated(
        PollingEventRequest<DateMemory> request)
        => HandlePagesPolling(request,
            x => x.LastEditedTime > request.Memory?.LastInteractionDate);

    [PollingEvent("On pages archived", "On any pages archived")]
    public Task<PollingEventResponse<DateMemory, ListPagesResponse>> OnPagesArchived(
        PollingEventRequest<DateMemory> request)
        => HandlePagesPolling(request,
            x => x.Archived is true && x.LastEditedTime > request.Memory?.LastInteractionDate);

    private async Task<PollingEventResponse<DateMemory, ListPagesResponse>> HandlePagesPolling(
        PollingEventRequest<DateMemory> request, Func<PageResponse, bool> filter)
    {
        if (request.Memory == null)
        {
            return new()
            {
                FlyBird = false,
                Memory = new()
                {
                    LastInteractionDate = DateTime.UtcNow
                }
            };
        }

        var items =
            (await Client.SearchAll<PageResponse>(Creds, "page"))
            .Where(filter)
            .ToArray();

        if (items.Length == 0)
            return new()
            {
                FlyBird = false,
                Memory = new()
                {
                    LastInteractionDate = DateTime.UtcNow
                }
            };

        return new()
        {
            FlyBird = true,
            Memory = new()
            {
                LastInteractionDate = DateTime.UtcNow
            },
            Result = new(items.Select(x => new PageEntity(x)).ToArray())
        };
    }
}