using Apps.Notion.Models.Entities;

namespace Apps.Notion.Models.Response.Page;

public record ListPagesResponse(PageEntity[] Pages);