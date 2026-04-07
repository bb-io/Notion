using Apps.NotionOAuth.DataSourceHandlers;
using Apps.NotionOAuth.Models.Request.DataBase;
using Apps.NotionOAuth.Models.Request.DataSource;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Tests.Notion.DataHandlerTests;

[TestClass]
public class ViewDataHandlerTests : BaseDataHandlerTests
{
    private readonly OptionalDatabaseRequest optionalDb = new() { DatabaseId = "33a3f4156cdc8092b465d7280d6b8bed" };
    private readonly OptionalDataSourceRequest optionalDs = new() { DataSourceId = "" };

    protected override IAsyncDataSourceItemHandler DataHandler => new ViewDataHandler(InvocationContext, optionalDb, optionalDs);

    protected override string SearchString => "what is";
}
