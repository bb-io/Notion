using Apps.Notion.Models.Entities;

namespace Apps.Notion.Models.Request.Page;

public class CreatePageRequest
{
    public ParentEntity Parent { get; set; }
    
    public object Properties { get; set; }
    
    public CreatePageRequest(CreatePageInput input)
    {
        Parent = new();

        if (input.PageId is not null)
            Parent.PageId = input.PageId;
        if (input.DatabaseId is not null)
            Parent.DatabaseId = input.DatabaseId;

        Properties = new();
    }
}