using Apps.NotionOAuth.DataSourceHandlers.DatabaseProperties;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Tests.Notion.DataHandlerTests;

[TestClass]
public class AllDatabasePropertyDataHandlerTests : BaseDataHandlerTests
{
    protected override IAsyncDataSourceItemHandler DataHandler => new AllDatabasePropertyDataHandler(
        InvocationContext,
        new() { DatabaseId = "326459b2-7801-40d9-8839-d4b47d50fceb" },
        new());

    protected override string SearchString => "Tickets";
}