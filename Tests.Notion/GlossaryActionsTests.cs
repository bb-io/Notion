using Tests.Notion.Base;
using Apps.NotionOAuth.Actions;
using Apps.NotionOAuth.Models.Request.DataSource;

namespace Tests.Notion;

[TestClass]
public class GlossaryActionsTests : TestBase
{
    private GlossaryActions _actions => new(InvocationContext, FileManager);

    [TestMethod]
    public async Task DownloadGlossary_NoOptionalInputs_works()
    {
        // Arrange
        var dataSource = new DataSourceRequest()
        {
            DataSourceId = "2d887f55-25f0-80ec-8fe5-000b81251270",
        };
        var input = new DownloadGlossaryRequest
        {
        };

        // Act
        var result = await _actions.DownloadGlossary(dataSource, input);

        // Assert
        Assert.AreEqual("Glossary.tbx", result.Glossary.Name);
    }

    [TestMethod]
    public async Task DownloadGlossary_AllInputs_works()
    {
        // Arrange
        var dataSource = new DataSourceRequest()
        {
            DataSourceId = "2d887f55-25f0-80ec-8fe5-000b81251270",
        };
        var input = new DownloadGlossaryRequest
        {
            PropertiesAsTargetLanguages = ["mFyX", "F%3EmZ"],
            Title = "Glossary Page",
            SourceDescription = "This is a sample glossary.",
            FilterFields = ["%3Ea%60b", "%3Ea%60b", "SPMh"],
            FilterValues = ["Approved", "Draft", "123@gmail.com"]
        };

        // Act
        var result = await _actions.DownloadGlossary(dataSource, input);

        // Assert
        Assert.AreEqual("Glossary Page.tbx", result.Glossary.Name);
    }
}
