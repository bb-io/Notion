using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.NotionOAuth.Utils;

public static class PluginMisconfigurationExceptionHelper
{
    public static void ThrowIfNullOrEmpty(string input, string? objectName = null)
    {
        if (!string.IsNullOrEmpty(input))
        {
            return;
        }

        var message = string.IsNullOrEmpty(objectName)
            ? "Specified input is null or empty. Please check this input and provide a valid (not empty) string."
            : $"Specified input ({objectName}) is null or empty. Please check this input and provide a valid (not empty) string.";

        throw new PluginMisconfigurationException(message);
    }
}