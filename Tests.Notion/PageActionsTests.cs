using Apps.NotionOAuth.Actions;
using Apps.NotionOAuth.Models.Request;
using Apps.NotionOAuth.Models.Request.Page;
using Apps.NotionOAuth.Models.Request.Page.Properties.Getter;
using Apps.NotionOAuth.Models.Request.Page.Properties.Setter;
using Blackbird.Applications.Sdk.Common.Files;
using Tests.Notion.Base;

namespace Tests.Notion;

[TestClass]
public class PageActionsTests : TestBase
{
    private PageActions _actions => new(InvocationContext, FileManager);

    [TestMethod]
    public async Task GetStringProperty_ValidParameters_ShouldReturnProperty()
    {
        // Arrange
        var input = new PageStringPropertyRequest
        {
            PageId = "21ca9644cf0281d381d0ecde5b6caace",
            DatabaseId = "218a9644cf0280b0b845cf1cc9645f12",
            PropertyId = "[KJ@",
        };

        // Act
        var page = await _actions.GetStringProperty(input);

        // Assert
        Console.WriteLine($"String property: '{page.PropertyValue}'");
    }

    [TestMethod]
    public async Task GetDateProperty_ValidParameters_ShouldReturnProperty()
    {
        // Arrange
        var input = new PageDatePropertyRequest
        {
            PageId = "2a2efdee-ad05-805c-8eb4-c37e3d18fbcb",
            DatabaseId = "21fefdee-ad05-802d-bfbc-e064ddc481fd",
            PropertyId = "hLss",
        };

        // Act
        var page = await _actions.GetDateProperty(input);

        // Assert
        Console.WriteLine($"String property: '{page.PropertyValue}'");
    }

    [TestMethod]
    public async Task GetStringProperty_EmptyValue_ShouldReturnEmptyString()
    {
        // Arrange
        var input = new PageStringPropertyRequest
        {
            PageId = "223a9644cf0280b5b89fe4ed27884218",
            DatabaseId = "218a9644cf0280b0b845cf1cc9645f12",
            PropertyId = "[KJ@",
        };

        // Act
        var page = await _actions.GetStringProperty(input);

        // Assert
        Console.WriteLine($"String property (empty expected): '{page.PropertyValue}'");
    }

    [TestMethod]
    public async Task GetRelatedPagesFromProperty_works()
    {
        // Arrange
        var input = new PageRelationPropertyRequest
        {
            DatabaseId = "36187e6f-6a33-4648-b9a9-4fde6c9e19f1",
            PageId = "23ba9644-cf02-8154-b2da-fb752c12371b",
            PropertyId = "w%3BQV"
        };

        // Act
        var pages = await _actions.GetRelatedPagesFromProperty(input);

        // Assert
        Console.WriteLine($"Related pages:");
        foreach (var page in pages.Pages)
        {
            Console.WriteLine($"{page.Id}: {page.Title}");
        }
    }

    [TestMethod]
    public async Task SetRelationProperty_works()
    {
        // Arrange
        var input = new SetPageRelationPropertyRequest
        {
            DatabaseId = "218a9644cf0280b0b845cf1cc9645f12",
            PageId = "218a9644cf0280688f7ee0fc26eecf1a",
            PropertyId = "jVGn",
            RelatedPageIds =
            [
                "21ca9644cf0281d381d0ecde5b6caace",
                "223a9644cf0280b5b89fe4ed27884218",
                "223a9644cf0280b28a35cbd660f4a491",
            ]
        };
        // Act
        await _actions.SetRelationProperty(input);

        // Assert
        Console.WriteLine($"Successfully set relation property for page {input.PageId}");
    }

    [TestMethod]
    public async Task GetPageAsHtml_ValidParameters_ShouldReturnHtmlFile()
    {
        // Arrange
        var pageId = "2e703abb8136811fb142df70b436bcf1";
        var pageRequest = new PageRequest
        {
            PageId = pageId
        };
        var htmlRequest = new GetPageAsHtmlRequest()
        {
        };

        // Act
        var result = await _actions.GetPageAsHtml(pageRequest, htmlRequest);

        // Assert
        Console.WriteLine($"HTML file name: {result.File.Name}, Size: {result.File.Size} bytes");
    }

    [TestMethod]
    public async Task CreatePageFromHtml_ValidParameters_ShouldCreatePage()
    {
        // Arrange
        var htmlFileName = "2e6efdee-ad05-8076-9461-cbba4a2d8056_en.html";
        
        var pageRequest = new CreatePageInput
        {
            Title = $"Test page: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            DatabaseId= "e5585fd5-8491-4cf1-9e1d-6ddfa93d2761"
        };
        
        var fileRequest = new FileRequest
        {
            File = new FileReference { Name = htmlFileName, ContentType = "text/html" }
        };

        // Act
        await _actions.CreatePageFromHtml(pageRequest, fileRequest);

        // Assert
        Console.WriteLine($"Successfully created page with HTML from {htmlFileName}");
    }
}
