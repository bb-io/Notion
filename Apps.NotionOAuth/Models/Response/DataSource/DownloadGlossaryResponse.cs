using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.NotionOAuth.Models.Response.DataSource;

public class DownloadGlossaryResponse
{
    [Display("Glossary")]
    public required FileReference Glossary { get; set; }

    [Display("Number of terms")]
    public required int NumberOfTerms { get; set; }
}
