namespace Apps.Notion.Models.Request;

public class FilterRequest
{
    public FilterRequest(string type, string? cursor)
    {
        StartCursor = cursor;
        Filter = new()
        {
            Value = type,
            Property = "object"
        };
    }

    public string? StartCursor { get; set; }

    public FilterConfig Filter { get; set; }
}