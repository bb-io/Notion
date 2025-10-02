using Apps.NotionOAuth.DataSourceHandlers;
using Apps.NotionOAuth.Models.Request.DataBase;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Tests.Notion.DataHandlerTests;

[TestClass]
public class DataSourceDataHandlerTests : BaseDataHandlerTests
{
    protected override IAsyncDataSourceItemHandler DataHandler => new DataSourceDataHandler(InvocationContext, new DatabaseRequest()
    {
        DatabaseId = "326459b2-7801-40d9-8839-d4b47d50fceb"
    });

    protected override string SearchString => "Tickets";
}