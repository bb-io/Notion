using Apps.NotionOAuth.Actions;
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
            DataSourceId = "e5585fd5-8491-4cf1-9e1d-6ddfa93d2761"
        };

        // Act
        var result = await action.SearchPagesInDatasource(input, new()
        {
        });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(ListPagesResponse));
        Assert.IsTrue(result.Pages.Length > 0, "Expected at least one page matching the filter.");

        foreach (var page in result.Pages)
        {
            Console.WriteLine($"Page ID: {page.Id}, Created Time: {page.CreatedTime}, Last Edited Time: {page.LastEditedTime}");
        }
    }
}