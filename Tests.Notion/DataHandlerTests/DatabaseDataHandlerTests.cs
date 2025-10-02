using Apps.NotionOAuth.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Tests.Notion.DataHandlerTests;

[TestClass]
public class DatabaseDataHandlerTests : BaseDataHandlerTests
{
    protected override IAsyncDataSourceItemHandler DataHandler => new DatabaseDataHandler(InvocationContext);

    protected override string SearchString => "Tickets";
}