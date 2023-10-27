namespace Apps.Notion.Models.Response.DataBase;

public class DatabaseResponse
{
    public string Id { get; set; }

    public DateTime CreatedTime { get; set; }
    
    public DateTime LastEditedTime { get; set; }
    
    public IEnumerable<TitleResponse> Title { get; set; }
    
    public Dictionary<string, PropertyResponse> Properties { get; set; }
    
    public DatabaseParentResponse Parent { get; set; }
    
    public string Url { get; set; }
    
    public bool Archived { get; set; }
}