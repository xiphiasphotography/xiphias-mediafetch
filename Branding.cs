using System.Reflection;

namespace XiPHiAS.MediaFetch;

internal static class Branding
{
    public const string GitHubUrl =
        "https://github.com/xiphiasphotography/xiphias-mediafetch";

    public static Image LoadLogo()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("XiPHiAS.MediaFetch.Logo.png")
            ?? throw new InvalidOperationException("Het ingebedde logo ontbreekt.");

        using var source = Image.FromStream(stream);
        return new Bitmap(source);
    }
}
