using Apps.NotionOAuth.Actions;
using Apps.NotionOAuth.Models.Request.Page.Properties.Getter;
using Tests.Notion.Base;

namespace Tests.Notion;

[TestClass]
public class PageActionsTests :TestBase
{
    [TestMethod]
    public async Task GetStringProperty_ValidParameters_ShouldReturnProperty()
    {
        // Arrange
        var pageId = "1b5efdee-ad05-8100-90af-f0471933c5e6";
        var action = new PageActions(InvocationContext,FileManager);
        var input = new PageStringPropertyRequest
        {
            PageId = pageId,
            DatabaseId = "18cefdee-ad05-80ab-a9fd-d1b5894d9d61",
            PropertyId = "Y_%5EN",
        };

        // Act
        var page = await action.GetStringProperty(input);

        // Assert
        Assert.IsNotNull(page);
        Console.WriteLine($"String property: {page.PropertyValue}");
    }
}
