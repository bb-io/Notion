using Apps.NotionOAuth.Models.Entities;

namespace Apps.NotionOAuth.Models.Response.Page;

public record ListPagesResponse(PageEntity[] Pages);