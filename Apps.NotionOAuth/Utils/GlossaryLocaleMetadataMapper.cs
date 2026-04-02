using Apps.NotionOAuth.Models.Response.Page;
using Blackbird.Applications.Sdk.Glossaries.Utils.Dtos;

namespace Apps.NotionOAuth.Utils;

internal static class GlossaryLocaleMetadataMapper
{
    private const string UsagePropertySuffix = "Usage";
    private const string NotesPropertySuffix = "Notes";
    private const string MatchTypePropertySuffix = "match type";
    private const string UsageTermNoteType = "usageNote";
    private const string ExactMatchTermNoteType = "exactMatch";

    public static void Apply(
        PageResponse page,
        string locale,
        GlossaryTermSection term,
        IReadOnlySet<string> existingPropertyNames)
    {
        if (TryGetLocalePropertyValue(page, existingPropertyNames, locale, UsagePropertySuffix, out var usage))
        {
            AddTermNote(term, UsageTermNoteType, usage);
        }

        if (TryGetLocalePropertyValue(page, existingPropertyNames, locale, NotesPropertySuffix, out var note))
        {
            term.Notes ??= [];
            term.Notes.Add(XmlHelper.SanitizeForXml(note));
        }

        if (TryGetLocalePropertyValue(page, existingPropertyNames, locale, MatchTypePropertySuffix, out var matchType)
            && TryConvertMatchTypeToExactMatch(matchType, out var exactMatch))
        {
            AddTermNote(term, ExactMatchTermNoteType, exactMatch);
        }
    }

    internal static bool TryConvertMatchTypeToExactMatch(string? matchType, out string exactMatch)
    {
        exactMatch = string.Empty;

        if (string.IsNullOrWhiteSpace(matchType))
        {
            return false;
        }

        var normalizedMatchType = matchType.Trim().ToLowerInvariant();

        switch (normalizedMatchType)
        {
            case "exact":
            case "exact match":
            case "true":
            case "yes":
            case "1":
                exactMatch = "True";
                return true;
            case "fuzzy":
            case "fuzzy match":
            case "false":
            case "no":
            case "0":
                exactMatch = "False";
                return true;
            default:
                return false;
        }
    }

    private static bool TryGetLocalePropertyValue(
        PageResponse page,
        IReadOnlySet<string> existingPropertyNames,
        string locale,
        string propertySuffix,
        out string value)
    {
        value = string.Empty;

        if (string.IsNullOrWhiteSpace(locale))
        {
            return false;
        }

        var propertyDisplayName = $"{locale} {propertySuffix}";
        if (!existingPropertyNames.Contains(propertyDisplayName))
        {
            return false;
        }

        return TryGetPropertyValueByName(page, propertyDisplayName, out value);
    }

    private static bool TryGetPropertyValueByName(
        PageResponse page,
        string propertyDisplayName,
        out string value)
    {
        value = string.Empty;

        if (string.IsNullOrWhiteSpace(propertyDisplayName) || page.Properties is null)
        {
            return false;
        }

        if (page.Properties.TryGetValue(propertyDisplayName, out var propertyObject) && propertyObject is not null)
        {
            value = PagePropertyParser.ToString(propertyObject);
            return !string.IsNullOrWhiteSpace(value);
        }

        var propertyMatch = page.Properties.FirstOrDefault(property =>
            string.Equals(property.Key, propertyDisplayName, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(propertyMatch.Key) || propertyMatch.Value is null)
        {
            return false;
        }

        value = PagePropertyParser.ToString(propertyMatch.Value);
        return !string.IsNullOrWhiteSpace(value);
    }

    private static void AddTermNote(GlossaryTermSection term, string termNoteType, string value)
    {
        term.TermNotes ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        term.TermNotes[termNoteType] = XmlHelper.SanitizeForXml(value);
    }
}
