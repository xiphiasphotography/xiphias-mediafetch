using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace XiPHiAS.MediaFetch;

internal sealed class YouTubeLoginDialog : Form
{
    private readonly WebView2 webView = new() { Dock = DockStyle.Fill };
    private readonly Label statusLabel = new()
    {
        Text = "WebView2 wordt gestart...",
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(12, 10, 0, 0)
    };
    private readonly Button useSessionButton = new()
    {
        Text = "Aanmelding gebruiken",
        Width = 165,
        Height = 34,
        Enabled = false
    };

    public YouTubeLoginDialog()
    {
        Text = "Aanmelden bij YouTube";
        ClientSize = new Size(980, 720);
        MinimumSize = new Size(720, 520);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            ColumnCount = 3,
            Padding = new Padding(8)
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
        useSessionButton.Click += UseSessionButton_Click;
        footer.Controls.Add(statusLabel, 0, 0);
        footer.Controls.Add(useSessionButton, 1, 0);
        footer.Controls.Add(cancelButton, 2, 0);

        CancelButton = cancelButton;
        Controls.Add(webView);
        Controls.Add(footer);
        Shown += InitializeWebViewAsync;
    }

    private async void InitializeWebViewAsync(object? sender, EventArgs e)
    {
        try
        {
            await YouTubeSessionManager.InitializeAsync(webView);
            webView.CoreWebView2.NavigationCompleted += NavigationCompleted;
            webView.CoreWebView2.Navigate(
                "https://accounts.google.com/ServiceLogin?service=youtube&continue=https%3A%2F%2Fwww.youtube.com%2F");
            statusLabel.Text = "Meld je aan en klik daarna op Aanmelding gebruiken.";
        }
        catch (WebView2RuntimeNotFoundException)
        {
            statusLabel.Text = "Microsoft Edge WebView2 Runtime ontbreekt.";
            MessageBox.Show(
                "De Microsoft Edge WebView2 Runtime is niet geïnstalleerd. Installeer deze runtime en probeer opnieuw.",
                "YouTube aanmelden",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            statusLabel.Text = "WebView2 kon niet worden gestart.";
            MessageBox.Show(ex.Message, "YouTube aanmelden",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void NavigationCompleted(
        object? sender,
        CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
            return;

        var cookies = await YouTubeSessionManager.ReadCookiesAsync(webView.CoreWebView2);
        useSessionButton.Enabled = YouTubeSessionManager.ContainsAuthenticationCookie(cookies);
        if (useSessionButton.Enabled)
            statusLabel.Text = "YouTube-aanmelding gevonden. Je kunt deze sessie gebruiken.";
    }

    private async void UseSessionButton_Click(object? sender, EventArgs e)
    {
        useSessionButton.Enabled = false;
        var cookies = await YouTubeSessionManager.ReadCookiesAsync(webView.CoreWebView2);
        if (!YouTubeSessionManager.ContainsAuthenticationCookie(cookies))
        {
            statusLabel.Text = "Nog geen geldige YouTube-aanmelding gevonden.";
            return;
        }

        YouTubeSessionManager.MarkSessionSaved();
        DialogResult = DialogResult.OK;
        Close();
    }
}
