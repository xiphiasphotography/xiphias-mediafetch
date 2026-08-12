using System.Net;
using System.Net.Http.Headers;

namespace XiPHiAS.MediaFetch;

public sealed class Downloader : IDisposable
{
    private readonly HttpClient _httpClient;

    public Downloader(
        string userAgent,
        Uri? referer = null)
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression =
                DecompressionMethods.GZip |
                DecompressionMethods.Deflate |
                DecompressionMethods.Brotli
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

        if (referer is not null)
        {
            _httpClient.DefaultRequestHeaders.Referrer = referer;
        }
    }

    public async Task DownloadAsync(
        DownloadItem item,
        IProgress<DownloadItem>? progress = null,
        CancellationToken cancellationToken = default)
    {
        const int maxRetries = 3;
        var downloadUrl = item.Url;
        var correctedUrl = CorrectUrlEncoding(item.Url);

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await DownloadInternalAsync(
                    item,
                    downloadUrl,
                    progress,
                    cancellationToken
                );

                return;
            }
            catch (OperationCanceledException)
            {
                item.Status = "Stopped";
                progress?.Report(item);
                throw;
            }
            catch (Exception ex)
            {
                var canTryCorrectedUrl =
                    attempt == 1 &&
                    !string.Equals(
                        correctedUrl,
                        item.Url,
                        StringComparison.Ordinal
                    );

                if (canTryCorrectedUrl)
                {
                    downloadUrl = correctedUrl;
                }

                item.Status =
                    attempt < maxRetries
                        ? canTryCorrectedUrl
                            ? "URL gecorrigeerd, opnieuw proberen"
                            : $"Retry {attempt}/{maxRetries}"
                        : $"Failed: {ex.Message}";

                progress?.Report(item);

                if (attempt >= maxRetries)
                    throw;

                if (!canTryCorrectedUrl)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(attempt * 2),
                        cancellationToken
                    );
                }
            }
        }
    }

    public async Task<long?> GetRemoteSizeAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken
        );
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(15));

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            AddAcceptHeaders(request, url);

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutSource.Token
            );

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return response.Content.Headers.ContentLength;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private async Task DownloadInternalAsync(
        DownloadItem item,
        string downloadUrl,
        IProgress<DownloadItem>? progress,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(item.DestinationPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        long existingLength = 0;

        if (
            !item.OverwriteExisting &&
            File.Exists(item.DestinationPath)
        )
        {
            existingLength =
                new FileInfo(item.DestinationPath).Length;
        }

        using var request =
            new HttpRequestMessage(HttpMethod.Get, downloadUrl);

        AddAcceptHeaders(request, downloadUrl);

        if (existingLength > 0)
        {
            request.Headers.Range =
                new RangeHeaderValue(existingLength, null);
        }

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );

        /*
         * Als de server geen resume ondersteunt en ondanks de Range-header
         * een volledige response (HTTP 200) terugstuurt, beginnen we opnieuw.
         */
        if (
            existingLength > 0 &&
            response.StatusCode == HttpStatusCode.OK
        )
        {
            existingLength = 0;
        }
        else
        {
            response.EnsureSuccessStatusCode();
        }

        var contentLength =
            response.Content.Headers.ContentLength;

        item.TotalBytes = contentLength.HasValue
            ? existingLength + contentLength.Value
            : null;

        item.BytesDownloaded = existingLength;
        item.BytesPerSecond = 0;
        item.EstimatedTimeRemaining = null;
        item.Status = existingLength > 0
            ? "Resuming"
            : "Downloading";

        progress?.Report(item);

        var fileMode =
            existingLength > 0
                ? FileMode.Append
                : FileMode.Create;

        await using var fileStream = new FileStream(
            item.DestinationPath,
            fileMode,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true
        );

        await using var networkStream =
            await response.Content.ReadAsStreamAsync(
                cancellationToken
            );

        var buffer = new byte[81920];
        var startedAt = System.Diagnostics.Stopwatch.StartNew();
        var transferredBytes = 0L;
        var lastProgressReport = TimeSpan.Zero;

        int bytesRead;

        while (
            (bytesRead = await networkStream.ReadAsync(
                buffer,
                cancellationToken
            )) > 0
        )
        {
            await fileStream.WriteAsync(
                buffer.AsMemory(0, bytesRead),
                cancellationToken
            );

            item.BytesDownloaded += bytesRead;
            transferredBytes += bytesRead;

            if (startedAt.Elapsed.TotalSeconds > 0)
            {
                item.BytesPerSecond =
                    transferredBytes / startedAt.Elapsed.TotalSeconds;

                if (
                    item.TotalBytes.HasValue &&
                    item.BytesPerSecond > 0
                )
                {
                    var remainingBytes = Math.Max(
                        0,
                        item.TotalBytes.Value - item.BytesDownloaded
                    );
                    item.EstimatedTimeRemaining = TimeSpan.FromSeconds(
                        remainingBytes / item.BytesPerSecond
                    );
                }
            }

            if (
                startedAt.Elapsed - lastProgressReport >=
                TimeSpan.FromMilliseconds(200)
            )
            {
                progress?.Report(item);
                lastProgressReport = startedAt.Elapsed;
            }
        }

        item.EstimatedTimeRemaining = TimeSpan.Zero;
        item.Status = "Completed";
        progress?.Report(item);
    }

    private static string CorrectUrlEncoding(string url)
    {
        var decodedUrl = WebUtility.HtmlDecode(url);

        if (!Uri.TryCreate(decodedUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            return url;
        }

        var encodedPath = string.Join(
            "/",
            uri.AbsolutePath
                .Split('/')
                .Select(segment => Uri.EscapeDataString(
                    Uri.UnescapeDataString(segment)
                ))
        );

        var builder = new UriBuilder(uri)
        {
            Path = encodedPath
        };

        return builder.Uri.AbsoluteUri;
    }

    private static void AddAcceptHeaders(
        HttpRequestMessage request,
        string url)
    {
        var requestsWebP = Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            string.Equals(
                Path.GetExtension(uri.AbsolutePath),
                ".webp",
                StringComparison.OrdinalIgnoreCase
            );

        if (requestsWebP)
        {
            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("image/webp")
            );
        }

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("image/png")
        );
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("image/jpeg")
        );
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("image/gif")
        );
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("video/*")
        );
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/*")
            {
                Quality = 0.8
            }
        );
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
