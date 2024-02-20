using Apps.NotionOAuth.Models.Entities;

namespace Apps.NotionOAuth.Models.Response.Block;

public record ListBlockChildrenResponse(BlockEntity[] Children);