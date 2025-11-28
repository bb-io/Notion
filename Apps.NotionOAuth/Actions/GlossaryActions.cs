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
        [ActionParameter] DownloadGlossaryRequest input)
    {
        input.DefaultLocale ??= "en";
        input.Title ??= "Glossary";
        input.SourceDescription ??= $"Glossary export from Notion on {DateTime.Now.ToUniversalTime():F} (UTC)";

        var dataSourceEndpoint = $"{ApiEndpoints.DataSources}/{input.DataSourceId}/query";
        var dataSourceRequest = new NotionRequest(dataSourceEndpoint, Method.Post, Creds);
        var dataSourceResponse = await Client.PaginateWithBody<PageResponse>(dataSourceRequest);

        if (dataSourceResponse.Count == 0)
            throw new PluginMisconfigurationException("The data source contains no entries.");

        var glossaryConceptEntries = new List<GlossaryConceptEntry>();

        foreach (var page in dataSourceResponse)
        {
            var languageSections = new List<GlossaryLanguageSection>();

            // Title of the page as source language
            var title = page.Properties?.Values
                .FirstOrDefault(p => p["id"]?.ToString() == "title")?
                .SelectToken("title[0].plain_text")?
                .ToString()
                ?? throw new Exception("[Download glossary] Page title was not found.");

            languageSections.Add(new(input.DefaultLocale, [new(title)]));

            // Other properties as target languages
            foreach (var propertyId in input.PropertiesAsTargetLanguages ?? [])
            {
                if (TryGetPropertyNameValue(page, propertyId, out var translation, out var locale))
                    languageSections.Add(new(locale, [new(translation)]));
            }

            // Create glossary entry
            var entry = new GlossaryConceptEntry(page.Id, languageSections);

            if (TryGetPropertyNameValue(page, input.DefinitionProperty, out var definition, out var _))
                entry.Definition = definition;

            if (TryGetPropertyNameValue(page, input.DomainProperty, out var domain, out var _))
                entry.Domain = domain;

            if (TryGetPropertyNameValue(page, input.NoteProperty, out var note, out var _))
            {
                entry.Notes ??= [];
                entry.Notes.Add(note);
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
}
