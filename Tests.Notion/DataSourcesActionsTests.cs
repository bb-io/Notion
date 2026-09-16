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
            DataSourceId = "b2e5c99e-5904-4ed4-91f5-fc049d6c60bc"
        };
        var database = new DatabaseRequest
        {
            
        };
        var searchInput = new SearchPagesInDataSourceRequest
        {
            
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