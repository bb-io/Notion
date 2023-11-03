using Apps.Notion.Models.Entities;

namespace Apps.Notion.Models.Response.Block;

public record ListBlockChildrenResponse(BlockEntity[] Children);