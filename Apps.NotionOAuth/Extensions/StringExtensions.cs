namespace Apps.NotionOAuth.Extensions;

public static class StringExtensions
{
    public static IEnumerable<string> ChunkString(this string content, int chunkSize)
    {
        if (string.IsNullOrEmpty(content))
            yield break;

        for (int i = 0; i < content.Length; i += chunkSize)
            yield return content.Substring(i, Math.Min(chunkSize, content.Length - i));
    }
}
