namespace Apps.NotionOAuth.Models.Response;

public class PaginationResponse<T>
{
    public IEnumerable<T> Results { get; set; }
    
    public string? NextCursor { get; set; }
    
    public bool HasMore { get; set; }
}