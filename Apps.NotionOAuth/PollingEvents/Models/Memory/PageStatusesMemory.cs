using Apps.NotionOAuth.Models.Entities;

namespace Apps.NotionOAuth.PollingEvents.Models.Memory;

public class PageStatusesMemory : DateMemory
{
    public List<PageStatusEntity> PageStatusEntities { get; set; } = new();
}

public class PageStatusEntity(string pageId, string pageStatus)
{
    public string PageId { get; set; } = pageId;

    public string PageStatus { get; set; } = pageStatus;

    public PageStatusEntity() : this(string.Empty, string.Empty)
    { }
}