using Apps.NotionOAuth.DataSourceHandlers;
using Apps.NotionOAuth.DataSourceHandlers.DatabaseProperties;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.NotionOAuth.Models.Request.DataSource;

public class DownloadGlossaryRequest
{
    [Display("Properties as target languages")]
    [DataSource(typeof(StringDatabasePropertyDataHandler))]
    public List<string>? PropertiesAsTargetLanguages { get; set; }

    [Display("Default locale", Description = "Title of the page will became a term. This option defines default locale. `en` by default.")]
    public string? DefaultLocale { get; set; }

    [Display("Property for term definition")]
    [DataSource(typeof(StringDatabasePropertyDataHandler))]
    public string? DefinitionProperty { get; set; }

    [Display("Property for term note")]
    [DataSource(typeof(StringDatabasePropertyDataHandler))]
    public string? NoteProperty { get; set; }

    [Display("Property for term domain")]
    [DataSource(typeof(StringDatabasePropertyDataHandler))]
    public string? DomainProperty { get; set; }

    [Display("Glossary title")]
    public string? Title { get; set; }

    [Display("Glossary source description")]
    public string? SourceDescription { get; set; }
}
