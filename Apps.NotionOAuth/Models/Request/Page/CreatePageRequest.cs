using Apps.NotionOAuth.Models.Entities;

namespace Apps.NotionOAuth.Models.Request.Page;

public class CreatePageRequest
{
    public ParentEntity Parent { get; set; }

    public Dictionary<string, Dictionary<string, object>> Properties { get; set; }

    public CreatePageRequest(CreatePageInput input)
    {
        Parent = new();

        if (input.PageId is not null)
            Parent.PageId = input.PageId;
        if (input.DatabaseId is not null)
            Parent.DatabaseId = input.DatabaseId;

        Properties = new()
        {
            ["title"] = new()
            {
                {
                    "title",
                    new[]
                    {
                        new TitleModel()
                        {
                            Text = new()
                            {
                                Content = input.Title
                            }
                        }
                    }
                }
            }
        };
    }
}