using System.Text.Json;

namespace XiPHiAS.MediaFetch;

internal sealed class AppSettings
{
    public const string DefaultUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
        "AppleWebKit/537.36 (KHTML, like Gecko) " +
        "Chrome/150.0.0.0 Safari/537.36";

    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "XiPHiAS",
        "MediaFetch"
    );

    private static readonly string SettingsFile = Path.Combine(
        SettingsDirectory,
        "settings.json"
    );

    public string? LastSourceDirectory { get; set; }
    public string? LastDestinationDirectory { get; set; }
    public string UserAgent { get; set; } = DefaultUserAgent;

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFile))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(SettingsFile);
            var settings = JsonSerializer.Deserialize<AppSettings>(json)
                ?? new AppSettings();

            if (string.IsNullOrWhiteSpace(settings.UserAgent))
            {
                settings.UserAgent = DefaultUserAgent;
            }

            return settings;
        }
        catch (IOException)
        {
            return new AppSettings();
        }
        catch (UnauthorizedAccessException)
        {
            return new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            var json = JsonSerializer.Serialize(
                this,
                new JsonSerializerOptions { WriteIndented = true }
            );
            File.WriteAllText(SettingsFile, json);
        }
        catch (IOException)
        {
            // Instellingen onthouden is nuttig, maar mag de app niet blokkeren.
        }
        catch (UnauthorizedAccessException)
        {
            // Instellingen onthouden is nuttig, maar mag de app niet blokkeren.
        }
    }
}
