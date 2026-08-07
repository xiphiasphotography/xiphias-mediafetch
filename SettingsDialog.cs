namespace XiPHiAS.MediaFetch;

internal sealed class SettingsDialog : Form
{
    private sealed record BrowserPreset(string Name, string UserAgent)
    {
        public override string ToString() => Name;
    }

    private static readonly BrowserPreset[] BrowserPresets =
    [
        new(
            "Google Chrome 150 (standaard)",
            AppSettings.DefaultUserAgent
        ),
        new(
            "Microsoft Edge 151",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/151.0.0.0 Safari/537.36 Edg/151.0.0.0"
        ),
        new(
            "Mozilla Firefox 152",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:152.0) " +
            "Gecko/20100101 Firefox/152.0"
        ),
        new(
            "Apple Safari 19",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) " +
            "AppleWebKit/605.1.15 (KHTML, like Gecko) " +
            "Version/19.0 Safari/605.1.15"
        ),
        new(
            "Opera 132",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/148.0.0.0 Safari/537.36 OPR/132.0.0.0"
        ),
        new(
            "Opera GX 132",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/148.0.0.0 Safari/537.36 OPR/132.0.0.0"
        ),
        new(
            "Brave 1.92 (Chromium 150)",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/150.0.0.0 Safari/537.36"
        ),
        new(
            "Internet Explorer 4",
            "Mozilla/4.0 (compatible; MSIE 4.0; Windows NT 5.1)"
        )
    ];

    private readonly TextBox userAgentTextBox;

    public string UserAgent => userAgentTextBox.Text.Trim();

    public SettingsDialog(string currentUserAgent)
    {
        Text = "Instellingen";
        ClientSize = new Size(720, 245);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;

        var presetLabel = new Label
        {
            Text = "Browserpreset:",
            Left = 20,
            Top = 20,
            Width = 120
        };

        var presetComboBox = new ComboBox
        {
            Left = 20,
            Top = 47,
            Width = 300,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        presetComboBox.Items.AddRange(BrowserPresets);

        var userAgentLabel = new Label
        {
            Text = "User-Agent:",
            Left = 20,
            Top = 90,
            Width = 100
        };

        userAgentTextBox = new TextBox
        {
            Left = 20,
            Top = 117,
            Width = 680,
            Text = currentUserAgent
        };

        presetComboBox.SelectedIndexChanged += (_, _) =>
        {
            if (presetComboBox.SelectedItem is BrowserPreset preset)
            {
                userAgentTextBox.Text = preset.UserAgent;
            }
        };

        var currentPresetIndex = Array.FindIndex(
            BrowserPresets,
            preset => preset.UserAgent == currentUserAgent
        );

        if (currentPresetIndex >= 0)
        {
            presetComboBox.SelectedIndex = currentPresetIndex;
        }

        var hint = new Label
        {
            Text = "De ingevulde User-Agent kan na het kiezen van een preset nog worden aangepast.",
            Left = 20,
            Top = 152,
            Width = 680
        };

        var saveButton = new Button
        {
            Text = "Opslaan",
            Left = 480,
            Top = 190,
            Width = 105,
            Height = 35,
            DialogResult = DialogResult.OK
        };

        var cancelButton = new Button
        {
            Text = "Annuleren",
            Left = 595,
            Top = 190,
            Width = 105,
            Height = 35,
            DialogResult = DialogResult.Cancel
        };

        AcceptButton = saveButton;
        CancelButton = cancelButton;
        Controls.AddRange([
            presetLabel,
            presetComboBox,
            userAgentLabel,
            userAgentTextBox,
            hint,
            saveButton,
            cancelButton
        ]);
    }
}
