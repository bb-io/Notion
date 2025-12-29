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
            DataSourceId = "cdc4b903-4226-476f-bed2-67c635cfac2a",
        };
        var input = new DownloadGlossaryRequest
        {
            NoteProperty = "uQDf",
            DefaultLocale = "en-US",
            DomainProperty = "Kqj%7C",
            DefinitionProperty = "b%5EAC",
            PropertiesAsTargetLanguages = ["DCWl", "%40%60%7CV"]
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
            DataSourceId = "2b8a9644-cf02-80e7-abed-000b68742d53"
        };
        var input = new DownloadGlossaryRequest
        {
            PropertiesAsTargetLanguages = ["yX%3BK", "%7C%3Do%3F", "UoW%40"], // pt-BR, es, de
            DefaultLocale = "en_US",
            DefinitionProperty = "VO%5Dw",
            NoteProperty = "%3FFhs",
            DomainProperty = "GZoJ",
            Title = "Glossary Page (all inputs)",
            SourceDescription = "This is a sample glossary."
        };

        // Act
        var result = await _actions.DownloadGlossary(dataSource, input);

        // Assert
        Assert.AreEqual("Glossary Page (all inputs).tbx", result.Glossary.Name);
    }
}
