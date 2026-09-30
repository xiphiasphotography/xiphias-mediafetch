using System.Text.RegularExpressions;

namespace XiPHiAS.MediaFetch;

internal static partial class FacebookFileNameFormatter
{
    public const string DefaultTemplate = "[N].[E]";
    public const string HotToysBloggerTemplate = "Z[C]-FB_IMG_[X].[E]";

    public static string Format(
        string template,
        string originalFileName,
        int counter,
        int digits,
        bool hotToysBloggerMode)
    {
        var extension = Path.GetExtension(originalFileName).TrimStart('.');
        var name = Path.GetFileNameWithoutExtension(originalFileName);
        var extracted = hotToysBloggerMode
            ? ExtractHotToysImageId(name) ?? name
            : name;

        var result = (string.IsNullOrWhiteSpace(template)
                ? DefaultTemplate
                : template)
            .Replace("[N]", name, StringComparison.OrdinalIgnoreCase)
            .Replace("[E]", extension, StringComparison.OrdinalIgnoreCase)
            .Replace("[C]", counter.ToString($"D{Math.Max(1, digits)}"),
                StringComparison.OrdinalIgnoreCase)
            .Replace("[X]", extracted, StringComparison.OrdinalIgnoreCase);

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            result = result.Replace(invalidChar, '_');
        }

        return string.IsNullOrWhiteSpace(result)
            ? originalFileName
            : result;
    }

    private static string? ExtractHotToysImageId(string name)
    {
        var match = HotToysFileNameRegex().Match(name);
        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex(@"^\d+_\d+_(\d+)_n$", RegexOptions.CultureInvariant)]
    private static partial Regex HotToysFileNameRegex();
}
