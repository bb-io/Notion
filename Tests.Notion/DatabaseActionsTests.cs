using Apps.NotionOAuth.Actions;
using Apps.NotionOAuth.Models.Request.DataBase;
using System.Text.Json;
using Tests.Notion.Base;

namespace Tests.Notion;

[TestClass]
public class DatabaseActionsTests : TestBase
{
    private DatabaseActions _actions => new(InvocationContext);

    private JsonSerializerOptions JsonOptions => new JsonSerializerOptions { WriteIndented = true };

    [TestMethod]
    public async Task SearchPagesInDatabase_works()
    {
        // Arrange
        var request = new SearchPagesInDatabaseRequest
        {
            DatabaseId = "36187e6f6a334648b9a94fde6c9e19f1",
            FilterProperty = "Type",
            FilterPropertyType = "select",
            FilterValue = "Solution architecting"
        };

        // Act
        var response = await _actions.SearchPagesInDatabase(request);

        // Assert
        Console.WriteLine($"Pages found:");
        foreach (var page in response.Pages)
        {
            Console.WriteLine($"{page.Id}: {page.Title}");
        }

        Console.WriteLine($"First page:");
        Console.WriteLine(JsonSerializer.Serialize(response.Pages.Skip(2).FirstOrDefault(), JsonOptions));

        Assert.IsTrue(response.Pages.Length > 0, "Expected at least one page in the response.");
    }
}
