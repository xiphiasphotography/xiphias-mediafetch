using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace XiPHiAS.MediaFetch;

internal sealed class FacebookAlbumResolverDialog : Form
{
    private static readonly string UserDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "XiPHiAS",
        "MediaFetch",
        "WebView2",
        "Facebook");

    private readonly string albumUrl;
    private readonly WebView2 webView = new() { Dock = DockStyle.Fill };
    private readonly Label statusLabel = new()
    {
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Text = "Facebook wordt gestart..."
    };
    private readonly Button analyzeButton = new()
    {
        Text = "Analyseren",
        Width = 140,
        Height = 34,
        Enabled = false
    };

    private bool busy;

    public IReadOnlyList<FacebookPhoto> Photos { get; private set; } = [];

    public FacebookAlbumResolverDialog(string albumUrl)
    {
        this.albumUrl = albumUrl;

        Text = "Facebook-album analyseren";
        ClientSize = new Size(1100, 780);
        MinimumSize = new Size(760, 560);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 58,
            Padding = new Padding(8),
            ColumnCount = 3
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var cancelButton = new Button
        {
            Text = "Annuleren",
            Width = 105,
            Height = 34,
            DialogResult = DialogResult.Cancel,
            Margin = new Padding(8, 0, 0, 0)
        };

        analyzeButton.Click += AnalyzeButton_Click;
        footer.Controls.Add(statusLabel, 0, 0);
        footer.Controls.Add(analyzeButton, 1, 0);
        footer.Controls.Add(cancelButton, 2, 0);

        CancelButton = cancelButton;
        Controls.Add(webView);
        Controls.Add(footer);
        Shown += InitializeAsync;
    }

    public static bool IsFacebookAlbumUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (!uri.Host.EndsWith("facebook.com", StringComparison.OrdinalIgnoreCase) &&
            !uri.Host.EndsWith("fb.com", StringComparison.OrdinalIgnoreCase))
            return false;

        return uri.AbsolutePath.Contains("/media/set", StringComparison.OrdinalIgnoreCase) &&
            uri.Query.Contains("set=", StringComparison.OrdinalIgnoreCase);
    }

    private async void InitializeAsync(object? sender, EventArgs e)
    {
        try
        {
            Directory.CreateDirectory(UserDataFolder);
            var environment = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: UserDataFolder);
            await webView.EnsureCoreWebView2Async(environment);

            webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webView.CoreWebView2.NavigationCompleted += (_, args) =>
            {
                if (args.IsSuccess && !busy)
                {
                    analyzeButton.Enabled = true;
                    statusLabel.Text =
                        "Meld je zo nodig aan bij Facebook en klik daarna op Album analyseren.";
                }
            };
            webView.CoreWebView2.Navigate(albumUrl);
        }
        catch (WebView2RuntimeNotFoundException)
        {
            MessageBox.Show(
                "De Microsoft Edge WebView2 Runtime is niet geïnstalleerd.",
                "Facebook-album",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            DialogResult = DialogResult.Cancel;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Facebook kon niet worden geopend.\n\n{ex.Message}",
                "Facebook-album",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    private async void AnalyzeButton_Click(object? sender, EventArgs e)
    {
        if (busy || webView.CoreWebView2 is null)
            return;

        busy = true;
        analyzeButton.Enabled = false;

        try
        {
            var photoLinks = await CollectAlbumPhotoLinksAsync();
            if (photoLinks.Count == 0)
            {
                MessageBox.Show(
                    "Er zijn geen foto-links gevonden. Controleer of het album zichtbaar is en of je bij Facebook bent aangemeld.",
                    "Facebook-album",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var photos = new List<FacebookPhoto>();
            for (var index = 0; index < photoLinks.Count; index++)
            {
                statusLabel.Text =
                    $"High-res foto zoeken: {index + 1} van {photoLinks.Count}...";

                var photo = await ResolvePhotoAsync(photoLinks[index], index);
                if (photo is not null)
                    photos.Add(photo);
            }

            if (photos.Count == 0)
            {
                MessageBox.Show(
                    "Facebook heeft geen bruikbare high-res afbeeldings-URL's opgeleverd.",
                    "Facebook-album",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            Photos = photos;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Het Facebook-album kon niet volledig worden geanalyseerd.\n\n{ex.Message}",
                "Facebook-album",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            busy = false;
            if (!IsDisposed)
                analyzeButton.Enabled = true;
        }
    }

    private async Task<List<string>> CollectAlbumPhotoLinksAsync()
    {
        statusLabel.Text = "Album doorlopen en foto's verzamelen...";

        var links = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stablePasses = 0;
        var previousCount = -1;

        var albumSet = GetQueryParameter(albumUrl, "set");

        for (var pass = 0; pass < 80 && stablePasses < 4; pass++)
        {
            var result = await webView.CoreWebView2.ExecuteScriptAsync("""
                (() => {
                    function collect(root) {
                        const result = [];

                        for (const a of root.querySelectorAll('a[href]')) {
                            const href = a.href || '';
                            if (
                                href.includes('/photo/?fbid=') ||
                                href.includes('/photo.php?fbid=') ||
                                (href.includes('/photos/') &&
                                 href.includes('facebook.com'))
                            ) {
                                result.push(href);
                            }
                        }

                        return result;
                    }

                    const albumContainer =
                        document.querySelector('div.html-div');
                    const scopedLinks = albumContainer
                        ? collect(albumContainer)
                        : [];

                    window.scrollTo(0, Math.max(
                        document.body.scrollHeight,
                        document.documentElement.scrollHeight
                    ));

                    return scopedLinks.length > 0
                        ? scopedLinks
                        : collect(document);
                })();
                """);

            var discoveredLinks = DeserializeStringArray(result);

            if (!string.IsNullOrWhiteSpace(albumSet))
            {
                var matchingAlbumLinks = discoveredLinks
                    .Where(link => string.Equals(
                        GetQueryParameter(link, "set"),
                        albumSet,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matchingAlbumLinks.Count > 0)
                {
                    discoveredLinks = matchingAlbumLinks;
                }
            }

            foreach (var link in discoveredLinks)
            {
                var key = GetPhotoKey(link);
                if (seen.Add(key))
                    links.Add(link);
            }

            if (links.Count == previousCount)
                stablePasses++;
            else
                stablePasses = 0;

            previousCount = links.Count;
            statusLabel.Text = $"{links.Count} foto-links gevonden...";
            await Task.Delay(700);
        }

        return links;
    }

    private async Task<FacebookPhoto?> ResolvePhotoAsync(
        string photoUrl,
        int albumIndex)
    {
        await NavigateAsync(photoUrl);
        await Task.Delay(650);

        var json = await webView.CoreWebView2.ExecuteScriptAsync("""
            (() => {
                function collect(root) {
                    const items = [];
                    const seen = new Set();

                    function add(url, width, height) {
                        if (!url || seen.has(url)) return;
                        if (!/fbcdn\.net|fbsbx\.com/i.test(url)) return;
                        if (!/\.(jpe?g|png|webp)(\?|$)/i.test(url)) return;
                        seen.add(url);
                        items.push({
                            url,
                            width: Number(width) || 0,
                            height: Number(height) || 0
                        });
                    }

                    for (const img of root.querySelectorAll('img')) {
                        add(
                            img.currentSrc || img.src,
                            img.naturalWidth,
                            img.naturalHeight
                        );

                        if (img.srcset) {
                            for (const part of img.srcset.split(',')) {
                                add(part.trim().split(/\s+/)[0], 0, 0);
                            }
                        }
                    }

                    return items;
                }

                const photoContainer = document.querySelector('div.html-div');
                const scopedItems = photoContainer ? collect(photoContainer) : [];

                if (scopedItems.length > 0) {
                    return scopedItems;
                }

                const pageItems = collect(document);

                if (pageItems.length > 0) {
                    return pageItems;
                }

                const resources = [];
                const seen = new Set();

                for (const entry of performance.getEntriesByType('resource')) {
                    const url = entry.name || '';
                    if (!url || seen.has(url)) continue;
                    if (!/fbcdn\.net|fbsbx\.com/i.test(url)) continue;
                    if (!/\.(jpe?g|png|webp)(\?|$)/i.test(url)) continue;

                    seen.add(url);
                    resources.push({
                        url,
                        width: 0,
                        height: 0
                    });
                }

                return resources;
            })();
            """);

        var candidates = JsonSerializer.Deserialize<List<ImageCandidate>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? [];

        var best = candidates
            .Where(candidate => Uri.TryCreate(
                candidate.Url,
                UriKind.Absolute,
                out _))
            .OrderByDescending(candidate =>
                (long)candidate.Width * candidate.Height)
            .ThenByDescending(candidate =>
                candidate.Url.Contains("stp=", StringComparison.OrdinalIgnoreCase)
                    ? 0
                    : 1)
            .ThenByDescending(candidate => candidate.Url.Length)
            .FirstOrDefault();

        if (best is null)
            return null;

        var uri = new Uri(best.Url);
        var fileName = Uri.UnescapeDataString(Path.GetFileName(uri.AbsolutePath));
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = $"facebook_{albumIndex + 1:D4}.jpg";

        return new FacebookPhoto
        {
            PhotoUrl = photoUrl,
            ResolvedUrl = best.Url,
            OriginalFileName = fileName,
            AlbumIndex = albumIndex,
            Width = best.Width,
            Height = best.Height
        };
    }

    private async Task NavigateAsync(string url)
    {
        var completion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        void Handler(
            object? sender,
            CoreWebView2NavigationCompletedEventArgs args)
        {
            webView.CoreWebView2.NavigationCompleted -= Handler;
            if (args.IsSuccess)
                completion.TrySetResult(true);
            else
                completion.TrySetException(
                    new InvalidOperationException(
                        $"Facebook-navigatie mislukt: {args.WebErrorStatus}"));
        }

        webView.CoreWebView2.NavigationCompleted += Handler;
        webView.CoreWebView2.Navigate(url);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var registration = timeout.Token.Register(() =>
        {
            webView.CoreWebView2.NavigationCompleted -= Handler;
            completion.TrySetException(
                new TimeoutException("Facebook reageerde niet binnen 20 seconden."));
        });

        await completion.Task;
    }

    private static string? GetQueryParameter(
        string url,
        string parameterName)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        foreach (var part in uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 2 &&
                pair[0].Equals(
                    parameterName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(pair[1]);
            }
        }

        return null;
    }

    private static string GetPhotoKey(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url;

        var query = uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in query)
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 2 &&
                pair[0].Equals("fbid", StringComparison.OrdinalIgnoreCase))
            {
                return $"fbid:{Uri.UnescapeDataString(pair[1])}";
            }
        }

        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
    }

    private static List<string> DeserializeStringArray(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed class ImageCandidate
    {
        public string Url { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
