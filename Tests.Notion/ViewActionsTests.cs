using Apps.NotionOAuth.Actions;
using Apps.NotionOAuth.Models.Request.DataBase;
using Apps.NotionOAuth.Models.Request.DataSource;
using Apps.NotionOAuth.Models.Request.View;
using Tests.Notion.Base;

namespace Tests.Notion;

[TestClass]
public class ViewActionsTests : TestBase
{
	private ViewActions Actions => new(InvocationContext);

	[TestMethod]
	public async Task SearchViews_ReturnsViews()
	{
		// Arrange
		var dbInput = new OptionalDatabaseRequest
		{
			DatabaseId = "33a3f4156cdc8092b465d7280d6b8bed",
        };
		var dsInput = new OptionalDataSourceRequest
        {
            //DataSourceId = "33a3f415-6cdc-8092-b465-d7280d6b8bed"
        };
		var filter = new SearchViewsRequest { ViewNameContains = "" };

		// Act
		var result = await Actions.SearchViews(dbInput, dsInput, filter);

		// Assert
		PrintJsonResult(result);
		Assert.IsNotNull(result);
	}

	[TestMethod]
	public async Task GetView_ReturnsView()
	{
        // Arrange
        var database = new OptionalDatabaseRequest();
		var dataSource = new OptionalDataSourceRequest();
        var viewInput = new ViewRequest { ViewId = "33b3f415-6cdc-80a4-93b4-000c9b8091c3" };

		// Act
		var result = await Actions.GetView(database, dataSource, viewInput);

		// Assert
		PrintJsonResult(result);
		Assert.IsNotNull(result);
	}

	[TestMethod]
	public async Task CreateView_ReturnsCreatedView()
	{
		// Arrange
		var dsInput = new DataSourceRequest 
		{ 
			DataSourceId = "33a3f415-6cdc-802e-ac52-000bd7779480",
        };
		var dbInput = new DatabaseRequest
		{
			DatabaseId = "33a3f4156cdc8092b465d7280d6b8bed"
        };
		var createInput = new CreateViewRequest
		{
			Name = "test from tests",
			Type = "list",
        };

        // Act
		var result = await Actions.CreateView(dsInput, dbInput, createInput);

		// Assert
		PrintJsonResult(result);
        Assert.IsNotNull(result);
    }

	[TestMethod]
	public async Task DeleteView_IsSuccess()
	{
		// Arrange
		var viewInput = new ViewRequest { ViewId = "33b3f415-6cdc-818a-bce7-000cfe804e40" };

		// Act
		await Actions.DeleteView(viewInput);
	}
}
