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
            DataSourceId = "49ddf508-2731-4b25-ab8a-208f168043c4"
        };

        // Act
        var result = await action.SearchPagesInDatasource(input, new()
        {
            FilterProperty = "Status",
            FilterPropertyType = "select",
            FilterValue = "Waiting for credentials"
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