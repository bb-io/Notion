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
            DataSourceId = "083a9644-cf02-83e6-91b2-07b6b1a0accb",
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
            DataSourceId = "083a9644-cf02-83e6-91b2-07b6b1a0accb"
        };
        var input = new DownloadGlossaryRequest
        {
            PropertiesAsTargetLanguages = ["UoW%40", "yX%3BK", "%7C%3Do%3F"],
            Title = "Glossary Page",
            SourceDescription = "This is a sample glossary.",
            DefaultLocale = "en",
            DefinitionProperty = "VO%5Dw",
            NoteProperty = "%7BOou",
            DomainProperty = "GZoJ",

            //FilterFields = ["%3Ea%60b", "%3Ea%60b", "SPMh"],
            //FilterValues = ["Approved", "Draft", "123@gmail.com"]
        };

        // Act
        var result = await _actions.DownloadGlossary(dataSource, input);

        // Assert
        Assert.AreEqual("Glossary Page.tbx", result.Glossary.Name);
    }
}
