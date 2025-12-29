using System.Text.RegularExpressions;

namespace Apps.NotionOAuth.Utils;

public static class XmlHelper
{
    public static string SanitizeForXml(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        // Removes control characters that are invalid in XML (0x00-0x1F) 
        // Preserves Tab (0x09), Line Feed (0x0A), Carriage Return (0x0D)
        return Regex.Replace(input, @"[\x00-\x08\x0B\x0C\x0E-\x1F]", "");
    }
}