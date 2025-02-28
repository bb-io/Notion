using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Entities;
using Apps.NotionOAuth.Models.Response.Page;
using Apps.NotionOAuth.PollingEvents.Models.Memory;
using Apps.NotionOAuth.PollingEvents.Models.Requests;
using Apps.NotionOAuth.Services;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;

namespace Apps.NotionOAuth.PollingEvents;

[PollingEventList]
public class PollingList(InvocationContext invocationContext) : NotionInvocable(invocationContext)
{
    [PollingEvent("On pages created", "Monitors pages whose has created within a specified time range.")]
    public Task<PollingEventResponse<DateMemory, ListPagesResponse>> OnPagesCreated(
        PollingEventRequest<DateMemory> request)
        => HandlePagesPolling(request,
            x => x.CreatedTime > request.Memory?.LastInteractionDate);

    [PollingEvent("On pages updated", "Monitors pages whose has updated within a specified time range.")]
    public Task<PollingEventResponse<DateMemory, ListPagesResponse>> OnPagesUpdated(
        PollingEventRequest<DateMemory> request)
        => HandlePagesPolling(request,
            x => x.LastEditedTime > request.Memory?.LastInteractionDate);

    [PollingEvent("On pages status changed",
        Description =
            "Monitors a database for pages whose status has changed to the desired value within a specified time range.")]
    public Task<PollingEventResponse<PageStatusesMemory, ListPagesResponse>> OnPagesStatusChanged(
        PollingEventRequest<PageStatusesMemory> request,
        [PollingEventParameter] QueryPagesInDatabaseRequest queryRequest) =>
        HandlePagesStatusChangedPolling(request, queryRequest);

    private async Task<PollingEventResponse<PageStatusesMemory, ListPagesResponse>> HandlePagesStatusChangedPolling(
        PollingEventRequest<PageStatusesMemory> request,
        QueryPagesInDatabaseRequest queryRequest)
    {
        var databaseService = new DatabaseService(InvocationContext);
        var pages = await databaseService.QueryPagesInDatabase(queryRequest);
        var pageStatusEntities =
            pages.Select(x => new PageStatusEntity(x.Id, queryRequest.StatusPropertyValue)).ToList();

        if (request.Memory == null)
        {
            return new()
            {
                FlyBird = false,
                Memory = new()
                {
                    LastInteractionDate = DateTime.UtcNow,
                    PageStatusEntities = pageStatusEntities
                }
            };
        }

        var memoryPageStatusEntities = request.Memory.PageStatusEntities;
        var pagesWithUpdatedStatuses = pageStatusEntities
            .Where(x => !memoryPageStatusEntities.Any(y => y.PageId == x.PageId && y.PageStatus == x.PageStatus))
            .ToList();

        var pageEntities = pages.Where(x => pagesWithUpdatedStatuses.Any(y => x.Id == y.PageId)).ToArray();
        return new()
        {
            FlyBird = pagesWithUpdatedStatuses.Any(),
            Result = new(pageEntities),
            Memory = new()
            {
                LastInteractionDate = DateTime.UtcNow,
                PageStatusEntities = pageStatusEntities
            }
        };
    }

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