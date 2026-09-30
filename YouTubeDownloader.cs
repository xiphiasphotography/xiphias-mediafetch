using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace XiPHiAS.MediaFetch;

internal sealed class YouTubeDownloader : IDisposable
{
    private sealed record StreamData(string Url, string Extension, long? Size,
        IReadOnlyDictionary<string, string> Headers);
    private sealed record VideoData(string Title, TimeSpan? Duration,
        StreamData Video, StreamData Audio, string? ThumbnailUrl);

    private static readonly SemaphoreSlim MuxSemaphore = new(1, 1);
    private readonly IReadOnlyList<Cookie> authenticationCookies;
    private readonly Dictionary<string, VideoData> preparedVideos = new(
        StringComparer.OrdinalIgnoreCase);
    private readonly HttpClient httpClient = new(new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.All
    }) { Timeout = Timeout.InfiniteTimeSpan };

    public YouTubeDownloader(IReadOnlyList<Cookie>? authenticationCookies = null) =>
        this.authenticationCookies = authenticationCookies ?? [];

    public static bool IsYouTubeUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        var host = uri.Host.TrimStart('.').ToLowerInvariant();
        return host is "youtu.be" or "youtube.com" or "www.youtube.com" or
            "m.youtube.com" or "music.youtube.com" or "youtube-nocookie.com" or
            "www.youtube-nocookie.com";
    }

    public async Task PrepareAsync(DownloadItem item, CancellationToken cancellationToken)
    {
        var data = await ExtractAsync(item.Url, cancellationToken);
        preparedVideos[item.QueueKey] = data;
        var safeTitle = MakeSafeFileName(data.Title);
        if (string.IsNullOrWhiteSpace(safeTitle)) safeTitle = $"youtube_{Guid.NewGuid():N}";
        var directory = Path.GetDirectoryName(item.DestinationPath) ?? string.Empty;
        item.IsYouTube = true;
        item.MediaDuration = data.Duration;
        item.FileName = $"{safeTitle}.mkv";
        item.DestinationPath = Path.Combine(directory, item.FileName);
    }

    public async Task DownloadAsync(DownloadItem item,
        IProgress<DownloadItem>? progress, CancellationToken cancellationToken)
    {
        if (!preparedVideos.TryGetValue(item.QueueKey, out var data))
            data = await ExtractAsync(item.Url, cancellationToken);

        item.MediaDuration = data.Duration;
        var directory = Path.GetDirectoryName(item.DestinationPath) ?? string.Empty;
        Directory.CreateDirectory(directory);
        var baseName = Path.GetFileNameWithoutExtension(item.DestinationPath);
        var videoPath = Path.Combine(directory, $"{baseName}.video.{data.Video.Extension}");
        var audioPath = Path.Combine(directory, $"{baseName}.audio.{data.Audio.Extension}");
        var thumbnailPath = Path.Combine(directory, $"{baseName}.jpg");

        if (!string.IsNullOrWhiteSpace(data.ThumbnailUrl))
        {
            try
            {
                await DownloadFileAsync(
                    new StreamData(data.ThumbnailUrl, "jpg", null, data.Video.Headers),
                    thumbnailPath, "Thumbnail downloaden", item, progress, cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch
            {
                item.Status = "Thumbnail overgeslagen";
                progress?.Report(item);
            }
        }

        await DownloadFileAsync(data.Video, videoPath, "Video downloaden", item,
            progress, cancellationToken);
        await DownloadFileAsync(data.Audio, audioPath, "Audio downloaden", item,
            progress, cancellationToken);
        await MuxAsync(videoPath, audioPath, item, progress, cancellationToken);
        item.ProgressOverride = 100;
        item.EstimatedTimeRemaining = TimeSpan.Zero;
        item.BytesPerSecond = 0;
        item.Status = "Completed";
        progress?.Report(item);
    }

    private async Task<VideoData> ExtractAsync(string url, CancellationToken cancellationToken)
    {
        var ytDlp = FindExecutable("yt-dlp.exe") ?? throw new FileNotFoundException(
            "yt-dlp is niet gevonden. Plaats yt-dlp.exe naast de app, in tools, of voeg het toe aan PATH.");
        var deno = FindExecutable("deno.exe") ?? throw new FileNotFoundException(
            "Deno is niet gevonden. Plaats deno.exe naast de app, in tools, of voeg het toe aan PATH.");

        try
        {
            return await RunExtractorAsync(ytDlp, deno, url, null, cancellationToken);
        }
        catch (Exception ex) when (
            authenticationCookies.Count > 0 && ex is not OperationCanceledException)
        {
            var cookiePath = await WriteCookieFileAsync(cancellationToken);
            try
            {
                return await RunExtractorAsync(ytDlp, deno, url, cookiePath, cancellationToken);
            }
            finally
            {
                try { File.Delete(cookiePath); } catch (IOException) { }
            }
        }
    }

    private static async Task<VideoData> RunExtractorAsync(string ytDlp, string deno,
        string url, string? cookiePath, CancellationToken cancellationToken)
    {
        var info = new ProcessStartInfo
        {
            FileName = ytDlp,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        foreach (var arg in new[] { "--no-playlist", "--no-warnings",
            "--dump-single-json", "--skip-download", "--js-runtimes",
            $"deno:{deno}", "-f", "bestvideo+bestaudio/best" })
            info.ArgumentList.Add(arg);
        if (cookiePath is not null)
        {
            info.ArgumentList.Add("--cookies");
            info.ArgumentList.Add(cookiePath);
        }
        info.ArgumentList.Add(url);

        using var process = Process.Start(info)
            ?? throw new InvalidOperationException("yt-dlp kon niet worden gestart.");
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var output = await outputTask;
        var error = await errorTask;
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"yt-dlp is mislukt: {LastLine(error)}");
        return ParseMetadata(output);
    }

    private static VideoData ParseMetadata(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var title = GetString(root, "title") ?? "YouTube-video";
        TimeSpan? duration = root.TryGetProperty("duration", out var durationValue) &&
            durationValue.TryGetDouble(out var seconds) ? TimeSpan.FromSeconds(seconds) : null;
        if (!root.TryGetProperty("requested_formats", out var formats) ||
            formats.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException(
                "yt-dlp heeft geen afzonderlijke video- en audiostream gevonden.");

        JsonElement? video = null;
        JsonElement? audio = null;
        foreach (var format in formats.EnumerateArray())
        {
            var vcodec = GetString(format, "vcodec");
            var acodec = GetString(format, "acodec");
            if (vcodec is not null and not "none" && acodec == "none") video = format.Clone();
            if (acodec is not null and not "none" && vcodec == "none") audio = format.Clone();
        }
        if (video is null || audio is null)
            throw new InvalidOperationException(
                "yt-dlp heeft geen afzonderlijke video- en audiostream gevonden.");

        return new VideoData(title, duration, ParseStream(video.Value),
            ParseStream(audio.Value), SelectThumbnail(root));
    }

    private static StreamData ParseStream(JsonElement format)
    {
        var url = GetString(format, "url")
            ?? throw new InvalidOperationException("yt-dlp gaf een stream zonder URL terug.");
        var extension = GetString(format, "ext") ?? "bin";
        long? size = null;
        if (format.TryGetProperty("filesize", out var value) && value.TryGetInt64(out var exact))
            size = exact;
        else if (format.TryGetProperty("filesize_approx", out value) &&
            value.TryGetInt64(out var approximate)) size = approximate;
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (format.TryGetProperty("http_headers", out var values) &&
            values.ValueKind == JsonValueKind.Object)
            foreach (var header in values.EnumerateObject())
                if (header.Value.ValueKind == JsonValueKind.String)
                    headers[header.Name] = header.Value.GetString()!;
        return new StreamData(url, extension, size, headers);
    }

    private static string? SelectThumbnail(JsonElement root)
    {
        if (!root.TryGetProperty("thumbnails", out var thumbnails) ||
            thumbnails.ValueKind != JsonValueKind.Array) return GetString(root, "thumbnail");
        return thumbnails.EnumerateArray()
            .Where(value => GetString(value, "url") is not null)
            .OrderByDescending(value =>
                (GetString(value, "url") ?? string.Empty).Contains(
                    ".jpg", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(value => GetLong(value, "width") * GetLong(value, "height"))
            .Select(value => GetString(value, "url")).FirstOrDefault();
    }

    private async Task DownloadFileAsync(StreamData stream, string path, string status,
        DownloadItem item, IProgress<DownloadItem>? progress,
        CancellationToken cancellationToken)
    {
        item.Status = status;
        item.TotalBytes = stream.Size;
        item.BytesDownloaded = 0;
        item.ProgressOverride = stream.Size.HasValue ? 0 : null;
        item.BytesPerSecond = 0;
        item.EstimatedTimeRemaining = null;
        progress?.Report(item);

        using var request = new HttpRequestMessage(HttpMethod.Get, stream.Url);
        foreach (var header in stream.Headers)
            if (!header.Key.Equals("Accept-Encoding", StringComparison.OrdinalIgnoreCase))
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        using var response = await httpClient.SendAsync(request,
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        item.TotalBytes ??= response.Content.Headers.ContentLength;

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = new FileStream(path, FileMode.Create, FileAccess.Write,
            FileShare.None, 81920, true);
        var buffer = new byte[81920];
        var stopwatch = Stopwatch.StartNew();
        var lastReport = TimeSpan.Zero;
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            item.BytesDownloaded += read;
            if (stopwatch.Elapsed.TotalSeconds > 0)
            {
                item.BytesPerSecond = item.BytesDownloaded / stopwatch.Elapsed.TotalSeconds;
                if (item.TotalBytes is > 0)
                {
                    item.ProgressOverride = (int)Math.Min(100,
                        item.BytesDownloaded * 100 / item.TotalBytes.Value);
                    var remaining = Math.Max(0, item.TotalBytes.Value - item.BytesDownloaded);
                    item.EstimatedTimeRemaining = item.BytesPerSecond > 0
                        ? TimeSpan.FromSeconds(remaining / item.BytesPerSecond) : null;
                }
            }
            if (stopwatch.Elapsed - lastReport >= TimeSpan.FromMilliseconds(200))
            {
                progress?.Report(item);
                lastReport = stopwatch.Elapsed;
            }
        }
        item.ProgressOverride = 100;
        progress?.Report(item);
    }

    private async Task<string> WriteCookieFileAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(Path.GetTempPath(), $"mediafetch-{Guid.NewGuid():N}.cookies.txt");
        var lines = new List<string> { "# Netscape HTTP Cookie File" };
        foreach (var cookie in authenticationCookies)
        {
            var domain = cookie.HttpOnly ? $"#HttpOnly_{cookie.Domain}" : cookie.Domain;
            var subdomains = cookie.Domain.StartsWith('.') ? "TRUE" : "FALSE";
            var expires = cookie.Expires == DateTime.MinValue ? 0 :
                new DateTimeOffset(cookie.Expires.ToUniversalTime()).ToUnixTimeSeconds();
            lines.Add(string.Join('\t', domain, subdomains, cookie.Path,
                cookie.Secure ? "TRUE" : "FALSE", expires, cookie.Name, cookie.Value));
        }
        await File.WriteAllLinesAsync(path, lines, new UTF8Encoding(false), cancellationToken);
        return path;
    }

    private static async Task MuxAsync(string videoPath, string audioPath,
        DownloadItem item, IProgress<DownloadItem>? progress,
        CancellationToken cancellationToken)
    {
        var ffmpeg = FindExecutable("ffmpeg.exe") ?? throw new FileNotFoundException(
            "FFmpeg is niet gevonden. Plaats ffmpeg.exe naast de app, in tools of ffmpeg, of voeg het toe aan PATH.");
        await MuxSemaphore.WaitAsync(cancellationToken);
        try
        {
            item.Status = "Samenvoegen";
            ResetProgress(item);
            progress?.Report(item);
            var stopwatch = Stopwatch.StartNew();
            var info = new ProcessStartInfo
            {
                FileName = ffmpeg,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (var arg in new[] { "-y", "-i", videoPath, "-i", audioPath,
                "-map", "0:v:0", "-map", "1:a:0", "-c", "copy", "-progress",
                "pipe:1", "-nostats", item.DestinationPath }) info.ArgumentList.Add(arg);
            using var process = Process.Start(info)
                ?? throw new InvalidOperationException("FFmpeg kon niet worden gestart.");
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            while (await process.StandardOutput.ReadLineAsync(cancellationToken) is { } line)
            {
                if (!line.StartsWith("out_time=", StringComparison.Ordinal) ||
                    !TimeSpan.TryParse(line[9..], CultureInfo.InvariantCulture, out var current) ||
                    item.MediaDuration is not { TotalSeconds: > 0 } duration) continue;
                var fraction = Math.Clamp(current.TotalSeconds / duration.TotalSeconds, 0, 1);
                item.ProgressOverride = (int)Math.Round(fraction * 100);
                item.EstimatedTimeRemaining = fraction > 0
                    ? TimeSpan.FromSeconds(stopwatch.Elapsed.TotalSeconds * (1 - fraction) / fraction)
                    : null;
                progress?.Report(item);
            }
            await process.WaitForExitAsync(cancellationToken);
            var error = await errorTask;
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"FFmpeg is mislukt: {LastLine(error)}");
        }
        finally { MuxSemaphore.Release(); }
    }

    private static void ResetProgress(DownloadItem item)
    {
        item.TotalBytes = null;
        item.BytesDownloaded = 0;
        item.BytesPerSecond = 0;
        item.EstimatedTimeRemaining = null;
        item.ProgressOverride = 0;
    }

    private static string? FindExecutable(string name)
    {
        var candidates = new[] { Path.Combine(AppContext.BaseDirectory, name),
            Path.Combine(AppContext.BaseDirectory, "tools", name),
            Path.Combine(AppContext.BaseDirectory, "ffmpeg", name) };
        foreach (var candidate in candidates) if (File.Exists(candidate)) return candidate;
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(directory.Trim('"'), name);
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() : null;
    private static long GetLong(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.TryGetInt64(out var result)
            ? result : 0;
    private static string LastLine(string value) => value
        .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
        .LastOrDefault(line => !string.IsNullOrWhiteSpace(line))?.Trim() ?? "onbekende fout";
    private static string MakeSafeFileName(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
        value = value.Trim().TrimEnd('.');
        return value.Length > 180 ? value[..180].TrimEnd().TrimEnd('.') : value;
    }

    public void Dispose() => httpClient.Dispose();
}
