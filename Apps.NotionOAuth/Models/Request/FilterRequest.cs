namespace Apps.NotionOAuth.Models.Request;

public class FilterRequest
{
    public FilterRequest(string type, string? cursor, string? query = null)
    {
        StartCursor = cursor;
        Query = query;
        Filter = new()
        {
            Value = type,
            Property = "object"
        };
    }

    public string? Query { get; set; }
    
    public string? StartCursor { get; set; }

    public FilterConfig Filter { get; set; }
}