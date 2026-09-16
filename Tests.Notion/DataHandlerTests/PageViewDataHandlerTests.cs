using Apps.NotionOAuth.DataSourceHandlers;
using Apps.NotionOAuth.Models.Request.DataBase;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Tests.Notion.DataHandlerTests;

[TestClass]
public class PageViewDataHandlerTests : BaseDataHandlerTests
{
    private readonly OptionalDatabaseRequest _dbInput = new() { DatabaseId = "fdca5bd2-9a32-4b15-8a0e-23971f1a9074" };
    
    protected override IAsyncDataSourceItemHandler DataHandler => new PageViewDataHandler(InvocationContext, _dbInput);

    protected override string SearchString => "what is";
}
