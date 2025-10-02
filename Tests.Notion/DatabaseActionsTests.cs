using Apps.NotionOAuth.Actions;
using Apps.NotionOAuth.Models.Request;
using Apps.NotionOAuth.Models.Request.DataBase;
using Apps.NotionOAuth.Models.Response.DataBase;
using Apps.NotionOAuth.Models.Response.Page;
using Tests.Notion.Base;

namespace Tests.Notion;

[TestClass]
public class DatabaseActionsTests : TestBase
{
    [TestMethod]
    public async Task ListDatabases_ValidRequest_Success()
    {
        // Arrange
        var action = new DatabaseActions(InvocationContext);
        var input = new ListRequest();

        // Act
        var result = await action.ListDatabases(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(ListDatabasesResponse));
        Assert.IsTrue(result.Databases.Length > 0, "Expected at least one database.");

        foreach (var db in result.Databases)
        {
            Console.WriteLine($"Database ID: {db.Id}, Title: {db.Title}, Created Time: {db.CreatedTime}, Last Edited Time: {db.LastEditedTime}");
        }
    }
    
    [TestMethod]
    public async Task SearchPagesInDatabase_ValidRequest_Success()
    {
        // Arrange
        var action = new DatabaseActions(InvocationContext);
        var input = new SearchPagesInDatabaseRequest
        {
            DatabaseId = "444a704b-78c0-409e-8299-ed011efc7bc8",
        };

        // Act
        var result = await action.SearchPagesInDatabase(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(ListPagesResponse));
        Assert.IsTrue(result.Pages.Length > 0, "Expected at least one page matching the filter.");

        foreach (var page in result.Pages)
        {
            Console.WriteLine($"Page ID: {page.Id}, Created Time: {page.CreatedTime}, Last Edited Time: {page.LastEditedTime}");
        }
    }
    
    [TestMethod]
    public async Task GetDatabase_ValidRequest_Success()
    {
        // Arrange
        var action = new DatabaseActions(InvocationContext);
        var input = new DatabaseRequest
        {
            DatabaseId = "444a704b-78c0-409e-8299-ed011efc7bc8",
        };

        // Act
        var result = await action.GetDatabase(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(input.DatabaseId, result.Id, "Database ID should match the requested ID.");

        Console.WriteLine($"Database ID: {result.Id}, Title: {result.Title}, Created Time: {result.CreatedTime}, Last Edited Time: {result.LastEditedTime}");
    }
    
    [TestMethod]
    public async Task CreateDatabase_ValidRequest_Success()
    {
        // Arrange
        var action = new DatabaseActions(InvocationContext);
        var input = new CreateDatabaseInput
        {
            PageId = "5d1ef0ea1edd4471b90fbf5791b4a98b",
            Title = "Test Database",
            Properties = [
            new()
            {
                Name = "Title",
                Type = "title"
            },
            new()
            {
                Name = "Description",
                Type = "rich_text"
            }]
        };

        // Act
        var result = await action.CreateDatabase(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(input.Title, result.Title, "Database title should match the requested title.");

        Console.WriteLine($"Created Database ID: {result.Id}, Title: {result.Title}, Created Time: {result.CreatedTime}, Last Edited Time: {result.LastEditedTime}");
    }
}