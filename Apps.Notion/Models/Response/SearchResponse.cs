namespace Apps.Notion.Models.Response;

public class SearchResponse<T>
{
    public IEnumerable<T> Results { get; set; }
    
    public string? NextCursor { get; set; }
}