﻿using Apps.NotionOAuth.Actions;
using Apps.NotionOAuth.Models.Request;
using Apps.NotionOAuth.Models.Request.Page;
using Apps.NotionOAuth.Models.Request.Page.Properties.Getter;
using Apps.NotionOAuth.Models.Request.Page.Properties.Setter;
using Blackbird.Applications.Sdk.Common.Files;
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

    [TestMethod]
    public async Task GetRelatedPagesFromProperty_works()
    {
        // Arrange
        var action = new PageActions(InvocationContext, FileManager);
        var input = new PageRelationPropertyRequest
        {
            DatabaseId = "36187e6f-6a33-4648-b9a9-4fde6c9e19f1",
            PageId = "23ba9644-cf02-8154-b2da-fb752c12371b",
            PropertyId = "w%3BQV"
        };

        // Act
        var pages = await action.GetRelatedPagesFromProperty(input);

        // Assert
        Console.WriteLine($"Related pages:");
        foreach (var page in pages.Pages)
        {
            Console.WriteLine($"{page.Id}: {page.Title}");
        }
        Assert.IsNotNull(pages.Pages);
    }

    [TestMethod]
    public async Task SetRelationProperty_works()
    {
        // Arrange
        var action = new PageActions(InvocationContext, FileManager);
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
        await action.SetRelationProperty(input);

        // Assert
        Console.WriteLine($"Successfully set relation property for page {input.PageId}");
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
