using Apps.NotionOAuth.Actions;
using Apps.NotionOAuth.Models.Request;
using Apps.NotionOAuth.Models.Request.Page;
using Apps.NotionOAuth.Models.Request.Page.Properties.Getter;
using Blackbird.Applications.Sdk.Common.Files;
using Tests.Notion.Base;

namespace Tests.Notion;

[TestClass]
public class PageActionsTests : TestBase
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

    [TestMethod]
    public async Task GetPageAsHtml_ValidParameters_ShouldReturnHtmlFile()
    {
        // Arrange
        var pageId = "21ca9644cf0280e19666c5bbbf0a7e8a";
        var action = new PageActions(InvocationContext, FileManager);
        var pageRequest = new PageRequest
        {
            PageId = pageId
        };
        var htmlRequest = new GetPageAsHtmlRequest();

        // Act
        var result = await action.GetPageAsHtml(pageRequest, htmlRequest);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.File);
        Console.WriteLine($"HTML file name: {result.File.Name}, Size: {result.File.Size} bytes");
    }

    [TestMethod]
    public async Task CreatePageFromHtml_ValidParameters_ShouldCreatePage()
    {
        // Arrange
        var pageId = "142a9644-cf02-80ca-a899-cf74abef21ec";
        var htmlFileName = "21ca9644cf0280e19666c5bbbf0a7e8a.html";
        var action = new PageActions(InvocationContext, FileManager);
        
        var pageRequest = new CreatePageInput
        {
            PageId = pageId,
            Title = $"Test page: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
        };
        
        // Create a mock file reference
        var file = new FileReference
        {
            Name = htmlFileName,
            ContentType = "text/html"
        };
        
        var fileRequest = new FileRequest
        {
            File = file
        };

        // Act & Assert
        await action.CreatePageFromHtml(pageRequest, fileRequest);
        Console.WriteLine($"Successfully updated page {pageId} with HTML from {htmlFileName}");
        Assert.IsTrue(true); // Test passes if no exception is thrown
    }
}
