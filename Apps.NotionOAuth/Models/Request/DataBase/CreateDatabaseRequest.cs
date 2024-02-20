using Apps.NotionOAuth.Models.Entities;

namespace Apps.NotionOAuth.Models.Request.DataBase;

public class CreateDatabaseRequest
{
    public ParentEntity Parent { get; set; }

    public Dictionary<string, Dictionary<string, object>> Properties { get; set; }

    public IEnumerable<TitleModel> Title { get; set; }

    public CreateDatabaseRequest(CreateDatabaseInput input)
    {
        Parent = new()
        {
            PageId = input.PageId
        };
        Title = new[]
        {
            new TitleModel()
            {
                Text = new()
                {
                    Content = input.Title
                }
            }
        };

        Properties = input.Properties
            .Select(x => new KeyValuePair<string, Dictionary<string, object>>(x.Name, new Dictionary<string, object>()
            {
                { x.Type, new() }
            })).ToDictionary(x => x.Key, x => x.Value);
    }
}