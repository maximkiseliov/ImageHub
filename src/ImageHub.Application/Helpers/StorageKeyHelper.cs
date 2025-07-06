using ImageHub.Application.Constants;
using System.Text.RegularExpressions;

namespace ImageHub.Application.Helpers;

public static class StorageKeyHelper
{
    private static readonly Regex InvalidCharsRegex = new(@"[^\w\-\.]", RegexOptions.Compiled);

    public static string GetKey(Guid id, string fileName, int height)
    {
        var normalizedFileName = InvalidCharsRegex.Replace(fileName, "_");
        return string.Format(ApplicationConstants.KeyFormat, id, height, normalizedFileName);
    }
}