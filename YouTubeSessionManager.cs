using System.Net;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace XiPHiAS.MediaFetch;

internal static class YouTubeSessionManager
{
    private static readonly string UserDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "XiPHiAS",
        "MediaFetch",
        "WebView2",
        "YouTube");

    private static readonly string SessionMarker = Path.Combine(
        UserDataFolder,
        ".session");

    public static bool HasSavedSession => File.Exists(SessionMarker);

    public static async Task<IReadOnlyList<Cookie>> GetCookiesAsync()
    {
        if (!HasSavedSession)
            return [];

        using var host = new Form
        {
            ShowInTaskbar = false,
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-32000, -32000),
            Size = new Size(1, 1),
            Opacity = 0
        };
        using var webView = new WebView2 { Dock = DockStyle.Fill };
        host.Controls.Add(webView);
        host.Show();

        await InitializeAsync(webView);
        return await ReadCookiesAsync(webView.CoreWebView2);
    }

    public static async Task<bool> HasValidSessionAsync()
    {
        if (!HasSavedSession)
            return false;

        var cookies = await GetCookiesAsync();
        return ContainsAuthenticationCookie(cookies);
    }

    public static async Task SignOutAsync()
    {
        if (Directory.Exists(UserDataFolder))
        {
            using var host = new Form
            {
                ShowInTaskbar = false,
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-32000, -32000),
                Size = new Size(1, 1),
                Opacity = 0
            };
            using var webView = new WebView2 { Dock = DockStyle.Fill };
            host.Controls.Add(webView);
            host.Show();
            await InitializeAsync(webView);
            webView.CoreWebView2.CookieManager.DeleteAllCookies();
        }

        if (File.Exists(SessionMarker))
            File.Delete(SessionMarker);
    }

    internal static async Task InitializeAsync(WebView2 webView)
    {
        Directory.CreateDirectory(UserDataFolder);
        var environment = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: UserDataFolder);
        await webView.EnsureCoreWebView2Async(environment);
    }

    internal static async Task<IReadOnlyList<Cookie>> ReadCookiesAsync(
        CoreWebView2 coreWebView)
    {
        // Een Google-aanmelding gebruikt cookies op meerdere Google- en
        // YouTube-domeinen. Met een lege URI geeft WebView2 het volledige,
        // uitsluitend voor MediaFetch bestemde profiel terug.
        var webViewCookies = await coreWebView.CookieManager.GetCookiesAsync(
            string.Empty);
        return webViewCookies
            .Where(cookie => cookie.IsSession || cookie.Expires > DateTime.Now)
            .Select(cookie =>
        {
            var result = new Cookie(
                cookie.Name,
                cookie.Value,
                string.IsNullOrWhiteSpace(cookie.Path) ? "/" : cookie.Path,
                cookie.Domain)
            {
                Secure = cookie.IsSecure,
                HttpOnly = cookie.IsHttpOnly
            };
            if (!cookie.IsSession && cookie.Expires > DateTime.MinValue)
                result.Expires = cookie.Expires;
            return result;
        }).ToList();
    }

    internal static bool ContainsAuthenticationCookie(
        IEnumerable<Cookie> cookies) => cookies.Any(cookie =>
            cookie.Name is "SID" or "SAPISID" or "__Secure-1PSID" or
                "__Secure-3PSID" or "LOGIN_INFO");

    internal static void MarkSessionSaved()
    {
        Directory.CreateDirectory(UserDataFolder);
        File.WriteAllText(SessionMarker, DateTimeOffset.Now.ToString("O"));
    }
}
