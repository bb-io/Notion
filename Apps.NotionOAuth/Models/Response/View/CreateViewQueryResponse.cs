using Apps.NotionOAuth.Models.Response;

namespace Apps.NotionOAuth.Models.Response.View;

public class CreateViewQueryResponse<T> : PaginationResponse<T>
{
    public string Id { get; set; } = string.Empty;
}
