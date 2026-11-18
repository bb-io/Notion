using Apps.NotionOAuth.Actions;
using Apps.NotionOAuth.Models.Request.DataBase;
using Apps.NotionOAuth.Models.Request.DataSource;
using Apps.NotionOAuth.Models.Response.Page;
using Tests.Notion.Base;

namespace Tests.Notion;

[TestClass]
public class DataSourcesActionsTests : TestBase
{
    [TestMethod]
    public async Task SearchPagesInDatasource_ValidRequest_Success()
    {
        // Arrange
        var action = new DataSourcesActions(InvocationContext);
        var input = new DataSourceRequest
        {
            DataSourceId = "da1c979e-627b-40c1-bc9a-6ce5b4ae3e9d"
        };
        var database = new OptionalDatabaseRequest
        {
            DatabaseId= "ebca274b-c142-4ed2-a7e8-7a3399d6d2e0"
        };
        var searchInput = new SearchPagesInDataSourceRequest
        {
            ViewId= "413a9644-cf02-80a7-b0a3-000c177e370a"
        };

        // Act
        var result = await action.SearchPagesInDatasource(input, database, searchInput);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(ListPagesResponse));
        Assert.IsTrue(result.Items.Count > 0, "Expected at least one page matching the filter.");
        Console.WriteLine($"Total items: {result.Items.Count}");

        foreach (var page in result.Items)
        {
            Console.WriteLine(
                $"Page ID: {page.ContentId}, " +
                $"Page Title: {page.Title}, " +
                $"Created Time: {page.CreatedTime}, " +
                $"Last Edited Time: {page.LastEditedTime}");
        }
    }
}