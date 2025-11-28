using Apps.NotionOAuth.Actions;
using Apps.NotionOAuth.Models.Request.DataSource;
using Apps.NotionOAuth.Models.Request.Page.Properties.Getter;
using Tests.Notion.Base;

namespace Tests.Notion;

[TestClass]
public class GlossaryActionsTests : TestBase
{
    private GlossaryActions _actions => new(InvocationContext, FileManager);

    [TestMethod]
    public async Task DownloadGlossary_NoOptionalInputs_works()
    {
        // Arrange
        var input = new DownloadGlossaryRequest
        {
            DataSourceId = "2b8a9644-cf02-80e7-abed-000b68742d53",
        };

        // Act
        var result = await _actions.DownloadGlossary(input);

        // Assert
        Assert.AreEqual("Glossary.tbx", result.Glossary.Name);
    }

    [TestMethod]
    public async Task DownloadGlossary_AllInputs_works()
    {
        // Arrange
        var input = new DownloadGlossaryRequest
        {
            DataSourceId = "2b8a9644-cf02-80e7-abed-000b68742d53",
            PropertiesAsTargetLanguages = ["yX%3BK", "%7C%3Do%3F", "UoW%40"], // pt-BR, es, de
            DefaultLocale = "en_US",
            DefinitionProperty = "VO%5Dw",
            NoteProperty = "%3FFhs",
            DomainProperty = "GZoJ",
            Title = "Glossary Page (all inputs)",
            SourceDescription = "This is a sample glossary."
        };

        // Act
        var result = await _actions.DownloadGlossary(input);

        // Assert
        Assert.AreEqual("Glossary Page (all inputs).tbx", result.Glossary.Name);
    }
}
