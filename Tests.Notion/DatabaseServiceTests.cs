using Apps.NotionOAuth.Models.Entities;
using Apps.NotionOAuth.PollingEvents.Models.Requests;
using Apps.NotionOAuth.Services;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Tests.Notion.Base;

namespace Tests.Notion;

[TestClass]
public class DatabaseServiceTests : TestBase
{
    private readonly DatabaseService _databaseService;

    public DatabaseServiceTests()
    {
        _databaseService = new DatabaseService(InvocationContext);
    }

    [TestMethod]
    public async Task QueryPagesInDatabase_ValidRequest_ShouldReturnPages()
    {
        // Arrange
        var expectedStatusPropertyValue = "Done";
        var queryRequest = new QueryPagesInDatabaseRequest
        {
            DatabaseId = "326459b2780140d98839d4b47d50fceb",
            StatusPropertyName = "Status",
            StatusPropertyType = "select",
            StatusPropertyValue = expectedStatusPropertyValue
        };

        // Act
        var result = await _databaseService.QueryPagesInDatabase(queryRequest);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(List<PageEntity>));
        Assert.IsTrue(result.Count > 0, "Expected at least one page entity.");

        foreach (var page in result)
        {
            var propertyValue = page.Properties.First(x => x.Name == "Status");
            Assert.IsTrue(propertyValue.Value.Equals(expectedStatusPropertyValue));
            Console.WriteLine($"{page.Title}: {propertyValue.Value}");
        }
    }

    [TestMethod]
    public async Task QueryPagesInDatabase_InvalidDatabaseId_ShouldThrowException()
    {
        // Arrange
        var queryRequest = new QueryPagesInDatabaseRequest
        {
            DatabaseId = "", // Invalid input
            StatusPropertyName = "Status",
            StatusPropertyType = "select",
            StatusPropertyValue = "Active"
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() => _databaseService.QueryPagesInDatabase(queryRequest));
    }

    [TestMethod]
    public async Task QueryPagesInDatabase_InvalidStatusProperty_ShouldThrowException()
    {
        // Arrange
        var queryRequest = new QueryPagesInDatabaseRequest
        {
            DatabaseId = "valid_database_id",
            StatusPropertyName = "", // Invalid input
            StatusPropertyType = "select",
            StatusPropertyValue = "Active"
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() => _databaseService.QueryPagesInDatabase(queryRequest));
    }

    [TestMethod]
    public async Task QueryPagesInDatabase_InvalidStatusPropertyType_ShouldThrowException()
    {
        // Arrange
        var queryRequest = new QueryPagesInDatabaseRequest
        {
            DatabaseId = "valid_database_id",
            StatusPropertyName = "Status",
            StatusPropertyType = "", // Invalid input
            StatusPropertyValue = "Active"
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() => _databaseService.QueryPagesInDatabase(queryRequest));
    }

    [TestMethod]
    public async Task QueryPagesInDatabase_InvalidStatusValue_ShouldThrowException()
    {
        // Arrange
        var queryRequest = new QueryPagesInDatabaseRequest
        {
            DatabaseId = "valid_database_id",
            StatusPropertyName = "Status",
            StatusPropertyType = "select",
            StatusPropertyValue = "" // Invalid input
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() => _databaseService.QueryPagesInDatabase(queryRequest));
    }
}