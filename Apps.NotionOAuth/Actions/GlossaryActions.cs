using Apps.NotionOAuth.Api;
using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Request.DataSource;
using Apps.NotionOAuth.Models.Response.DataSource;
using Apps.NotionOAuth.Models.Response.Page;
using Apps.NotionOAuth.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Glossaries.Utils.Converters;
using Blackbird.Applications.Sdk.Glossaries.Utils.Dtos;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using RestSharp;
using System.Net.Mime;

namespace Apps.NotionOAuth.Actions;

[ActionList("Glossaries")]
public class GlossaryActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : NotionInvocable(invocationContext)
{
    [Action("Download glossary", Description = "Downloads database pages into an interoperable glosary.")]
    public async Task<DownloadGlossaryResponse> DownloadGlossary(
        [ActionParameter] DataSourceRequest dataSource,
        [ActionParameter] DownloadGlossaryRequest input)
    {
        var fieldsCount = input.FilterFields?.Count ?? 0;
        var valuesCount = input.FilterValues?.Count ?? 0;

        if (fieldsCount != valuesCount)
        {
            throw new PluginMisconfigurationException(
                $"You provided {fieldsCount} filter fields but {valuesCount} filter values. " +
                $"These lists must be the same length."
            );
        }

        input.DefaultLocale ??= "en";
        input.Title ??= "Glossary";
        input.SourceDescription ??= $"Glossary export from Notion on {DateTime.Now.ToUniversalTime():F} (UTC)";

        var dataSourceEndpoint = $"{ApiEndpoints.DataSources}/{dataSource.DataSourceId}/query";
        var dataSourceRequest = new NotionRequest(dataSourceEndpoint, Method.Post, Creds);
        var dataSourceResponse = await Client.PaginateWithBody<PageResponse>(dataSourceRequest);

        if (dataSourceResponse.Count == 0)
            throw new PluginMisconfigurationException("The data source contains no entries.");

        var existingPropertyNames = dataSourceResponse.First().Properties?.Keys
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);


        var glossaryConceptEntries = new List<GlossaryConceptEntry>();

        var filterGroups = (input.FilterFields ?? [])
            .Zip(input.FilterValues ?? [], (field, value) => new { FieldId = field, Value = value })
            .GroupBy(x => x.FieldId)
            .ToList();

        foreach (var page in dataSourceResponse)
        {
            bool matchesFilters = false;

            var filterFields = input.FilterFields?.ToList() ?? [];
            var filterValues = input.FilterValues?.ToList() ?? [];

            if (filterFields.Count == 0)
                matchesFilters = true;
            else
            {
                for (int i = 0; i < filterFields.Count; i++)
                {
                    var fieldId = filterFields[i];
                    var expectedValue = filterValues.ElementAtOrDefault(i);

                    if (TryGetPropertyNameValue(page, fieldId, out var actualValue, out var _))
                    {
                        if (actualValue.Equals(expectedValue, StringComparison.OrdinalIgnoreCase))
                        {
                            matchesFilters = true;
                            break;
                        }
                    }
                }
            }

            if (!matchesFilters) continue;

            var languageSections = new List<GlossaryLanguageSection>();

            // Title of the page as source language
            var rawTitle = page.Properties?.Values
                .FirstOrDefault(p => p["id"]?.ToString() == "title")?
                .SelectToken("title[0].plain_text")?
                .ToString()
                ?? throw new Exception("[Download glossary] Page title was not found.");
            var title = XmlHelper.SanitizeForXml(rawTitle);

            var sourceTerm = new GlossaryTermSection(title);
            ApplyLocaleUsageAndNotesIfExist(page, input.DefaultLocale, sourceTerm, existingPropertyNames);

            if (string.IsNullOrWhiteSpace(sourceTerm.UsageExample) && TryGetPropertyValueByName(page, "Use cases", out var useCases))
            {
                sourceTerm.UsageExample = XmlHelper.SanitizeForXml(useCases);
            }

            languageSections.Add(new GlossaryLanguageSection(input.DefaultLocale, [sourceTerm]));

            // Other properties as target languages
            foreach (var propertyId in input.PropertiesAsTargetLanguages ?? [])
            {
                if (!TryGetPropertyNameValue(page, propertyId, out var translation, out var locale))
                    continue;

                var term = new GlossaryTermSection(XmlHelper.SanitizeForXml(translation));
                ApplyLocaleUsageAndNotesIfExist(page, locale, term, existingPropertyNames);

                languageSections.Add(new GlossaryLanguageSection(locale, [term]));
            }

            // Create glossary entry
            var entry = new GlossaryConceptEntry(page.Id, languageSections);

            if (TryGetPropertyNameValue(page, input.DefinitionProperty, out var definition, out var _))
                entry.Definition = XmlHelper.SanitizeForXml(definition);

            if (TryGetPropertyNameValue(page, input.DomainProperty, out var domain, out var _))
                entry.Domain = XmlHelper.SanitizeForXml(domain);

            if (TryGetPropertyNameValue(page, input.NoteProperty, out var note, out var _))
            {
                entry.Notes ??= [];
                entry.Notes.Add(XmlHelper.SanitizeForXml(note));
            }

            glossaryConceptEntries.Add(entry);
        }

        var glossary = new Glossary(glossaryConceptEntries)
        {
            Title = input.Title,
            SourceDescription = input.SourceDescription,
        };

        var glossaryFileName = $"{input.Title}.tbx";
        await using var glossaryStream = glossary.ConvertToTbx();
        var glossaryFile = await fileManagementClient.UploadAsync(glossaryStream, MediaTypeNames.Application.Xml, glossaryFileName);

        return new()
        {
            Glossary = glossaryFile,
            NumberOfTerms = glossaryConceptEntries.Count
        };
    }

    private static bool TryGetPropertyNameValue(
        PageResponse page,
        string? propertyId,
        out string propertyValue,
        out string propertyName)
    {
        propertyValue = string.Empty;
        propertyName = string.Empty;

        if (string.IsNullOrWhiteSpace(propertyId))
            return false;

        var translationProperty = page.Properties?
                    .FirstOrDefault(p => p.Value["id"]?.ToString() == propertyId);

        if (translationProperty is null)
            return false;

        propertyName = translationProperty.Value.Key;
        propertyValue = PagePropertyParser.ToString(translationProperty.Value.Value);

        if (string.IsNullOrWhiteSpace(propertyValue))
            return false;

        return true;
    }

    private static bool TryGetPropertyValueByName(
     PageResponse page,
     string propertyDisplayName,
     out string value)
    {
        value = string.Empty;

        if (string.IsNullOrWhiteSpace(propertyDisplayName))
            return false;

        if (page.Properties is null)
            return false;

        if (page.Properties.TryGetValue(propertyDisplayName, out var propObj) && propObj is not null)
        {
            value = PagePropertyParser.ToString(propObj);
            return !string.IsNullOrWhiteSpace(value);
        }

        var match = page.Properties
            .FirstOrDefault(kvp => string.Equals(kvp.Key, propertyDisplayName, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(match.Key) || match.Value is null)
            return false;

        value = PagePropertyParser.ToString(match.Value);
        return !string.IsNullOrWhiteSpace(value);
    }

    private static void ApplyLocaleUsageAndNotesIfExist(
    PageResponse page,
    string locale,
    GlossaryTermSection term,
    HashSet<string> existingPropertyNames)
    {
        var usageProp = $"{locale} Usage";
        if (existingPropertyNames.Contains(usageProp) &&
            TryGetPropertyValueByName(page, usageProp, out var usage))
        {
            term.UsageExample = XmlHelper.SanitizeForXml(usage);
        }

        var notesProp = $"{locale} Notes";
        if (existingPropertyNames.Contains(notesProp) &&
            TryGetPropertyValueByName(page, notesProp, out var note))
        {
            term.Notes ??= [];
            term.Notes.Add(XmlHelper.SanitizeForXml(note));
        }
    }
}
