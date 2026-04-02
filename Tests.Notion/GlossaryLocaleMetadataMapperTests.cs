using Apps.NotionOAuth.Models.Response.Page;
using Apps.NotionOAuth.Utils;
using Blackbird.Applications.Sdk.Glossaries.Utils.Converters;
using Blackbird.Applications.Sdk.Glossaries.Utils.Dtos;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;

namespace Tests.Notion;

[TestClass]
public class GlossaryLocaleMetadataMapperTests
{
    [TestMethod]
    public void Apply_RegionalSourceLocaleMetadata_AddsUsageNotesAndExactMatch()
    {
        var page = CreatePageResponse(new Dictionary<string, string>
        {
            ["en-US Usage"] = "Use ROI in finance dashboards.",
            ["en-US Notes"] = "Keep the abbreviation unchanged.",
            ["en-US match type"] = "Exact"
        });
        var existingPropertyNames = page.Properties!.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var term = new GlossaryTermSection("ROI");

        GlossaryLocaleMetadataMapper.Apply(page, "en-us", term, existingPropertyNames);

        Assert.IsNotNull(term.TermNotes);
        Assert.AreEqual("Use ROI in finance dashboards.", term.TermNotes["usageNote"]);
        Assert.AreEqual("True", term.TermNotes["exactMatch"]);
        CollectionAssert.AreEqual(new[] { "Keep the abbreviation unchanged." }, term.Notes!.ToList());
    }

    [TestMethod]
    public void Apply_FuzzyMatchType_MapsToFalseExactMatchValue()
    {
        var page = CreatePageResponse(new Dictionary<string, string>
        {
            ["pt-BR match type"] = "Fuzzy"
        });
        var existingPropertyNames = page.Properties!.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var term = new GlossaryTermSection("API");

        GlossaryLocaleMetadataMapper.Apply(page, "pt-BR", term, existingPropertyNames);

        Assert.IsNotNull(term.TermNotes);
        Assert.AreEqual("False", term.TermNotes["exactMatch"]);
    }

    [TestMethod]
    public void TryConvertMatchTypeToExactMatch_UnsupportedValue_ReturnsFalse()
    {
        var converted = GlossaryLocaleMetadataMapper.TryConvertMatchTypeToExactMatch("Context", out var exactMatch);

        Assert.IsFalse(converted);
        Assert.AreEqual(string.Empty, exactMatch);
    }

    [TestMethod]
    public async Task Apply_WhenConvertedToTbx_WritesPhraseCompatibleLocaleMetadata()
    {
        var page = CreatePageResponse(new Dictionary<string, string>
        {
            ["en-US Usage"] = "Use ROI in finance dashboards.",
            ["en-US Notes"] = "Keep the abbreviation unchanged.",
            ["en-US match type"] = "Exact",
            ["es match type"] = "Fuzzy"
        });
        var existingPropertyNames = page.Properties!.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var sourceTerm = new GlossaryTermSection("ROI");
        GlossaryLocaleMetadataMapper.Apply(page, "en-us", sourceTerm, existingPropertyNames);

        var targetTerm = new GlossaryTermSection("ROI [es]");
        GlossaryLocaleMetadataMapper.Apply(page, "es", targetTerm, existingPropertyNames);

        var glossary = new Glossary([
            new GlossaryConceptEntry("concept-1", [
                new GlossaryLanguageSection("en-us", [sourceTerm]),
                new GlossaryLanguageSection("es", [targetTerm])
            ])
        ])
        {
            Title = "Glossary",
            SourceDescription = "Glossary export test."
        };

        await using var glossaryStream = glossary.ConvertToTbx();
        glossaryStream.Position = 0;

        var tbxDocument = await XDocument.LoadAsync(glossaryStream, LoadOptions.None, CancellationToken.None);
        var tbxNamespace = XNamespace.Get("urn:iso:std:iso:30042:ed-2");

        var sourceTermSection = GetTermSection(tbxDocument, tbxNamespace, "en-us");
        AssertTermNoteValue(sourceTermSection, tbxNamespace, "usageNote", "Use ROI in finance dashboards.");
        AssertTermNoteValue(sourceTermSection, tbxNamespace, "exactMatch", "True");
        Assert.AreEqual("Keep the abbreviation unchanged.", sourceTermSection.Element(tbxNamespace + "note")?.Value);

        var targetTermSection = GetTermSection(tbxDocument, tbxNamespace, "es");
        AssertTermNoteValue(targetTermSection, tbxNamespace, "exactMatch", "False");
    }

    private static PageResponse CreatePageResponse(Dictionary<string, string> properties)
    {
        return new PageResponse
        {
            Properties = properties.ToDictionary(
                property => property.Key,
                property => CreateRichTextProperty(property.Value))
        };
    }

    private static JObject CreateRichTextProperty(string value)
    {
        return new JObject
        {
            ["id"] = Guid.NewGuid().ToString("N"),
            ["type"] = "rich_text",
            ["rich_text"] = new JArray(
                new JObject
                {
                    ["type"] = "text",
                    ["plain_text"] = value,
                    ["text"] = new JObject
                    {
                        ["content"] = value
                    }
                })
        };
    }

    private static XElement GetTermSection(XDocument tbxDocument, XNamespace tbxNamespace, string locale)
    {
        return tbxDocument
            .Descendants(tbxNamespace + "langSec")
            .Single(languageSection => string.Equals(
                languageSection.Attribute(XNamespace.Xml + "lang")?.Value,
                locale,
                StringComparison.OrdinalIgnoreCase))
            .Element(tbxNamespace + "termSec")!;
    }

    private static void AssertTermNoteValue(
        XElement termSection,
        XNamespace tbxNamespace,
        string termNoteType,
        string expectedValue)
    {
        var termNote = termSection
            .Elements(tbxNamespace + "termNote")
            .Single(note => string.Equals(note.Attribute("type")?.Value, termNoteType, StringComparison.OrdinalIgnoreCase));

        Assert.AreEqual(expectedValue, termNote.Value);
    }
}
