namespace XiPHiAS.MediaFetch;

internal sealed class FacebookAlbumDialog : Form
{
    private readonly TextBox txtDestination = new();
    private readonly TextBox txtFileName = new();
    private readonly CheckBox chkHotToys = new();
    private readonly NumericUpDown numStart = new();
    private readonly NumericUpDown numDigits = new();
    private readonly Label lblPreview = new();

    private bool applyingPreset;

    public string DestinationDirectory => txtDestination.Text.Trim();
    public string FileNameTemplate => txtFileName.Text.Trim();
    public bool HotToysBloggerMode => chkHotToys.Checked;
    public int StartNumber => (int)numStart.Value;
    public int CounterDigits => (int)numDigits.Value;

    public FacebookAlbumDialog(
        string albumUrl,
        string initialDestination,
        string initialTemplate,
        bool hotToysBloggerMode,
        int counterDigits)
    {
        Text = "Facebook-album downloaden";
        ClientSize = new Size(700, 390);
        MinimumSize = new Size(640, 390);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 3,
            RowCount = 8
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));

        var txtAlbum = new TextBox
        {
            ReadOnly = true,
            Text = albumUrl,
            Dock = DockStyle.Fill
        };
        txtDestination.Text = initialDestination;
        txtDestination.Dock = DockStyle.Fill;
        txtFileName.Text = string.IsNullOrWhiteSpace(initialTemplate)
            ? FacebookFileNameFormatter.DefaultTemplate
            : initialTemplate;
        txtFileName.Dock = DockStyle.Fill;

        chkHotToys.Text = "Hot Toys Blogger-foto's";
        chkHotToys.AutoSize = true;
        chkHotToys.Checked = hotToysBloggerMode;

        numStart.Minimum = 0;
        numStart.Maximum = 999999999;
        numStart.Value = 1;
        numStart.Width = 110;

        numDigits.Minimum = 1;
        numDigits.Maximum = 12;
        numDigits.Value = Math.Clamp(counterDigits, 1, 12);
        numDigits.Width = 110;

        var browseButton = new Button
        {
            Text = "…",
            Dock = DockStyle.Fill,
            Margin = new Padding(3, 0, 0, 0)
        };
        browseButton.Click += (_, _) => BrowseDestination();

        lblPreview.AutoSize = true;
        lblPreview.ForeColor = SystemColors.GrayText;
        lblPreview.Margin = new Padding(3, 8, 3, 8);

        layout.Controls.Add(CreateLabel("Album:"), 0, 0);
        layout.Controls.Add(txtAlbum, 1, 0);
        layout.SetColumnSpan(txtAlbum, 2);

        layout.Controls.Add(CreateLabel("Doelmap:"), 0, 1);
        layout.Controls.Add(txtDestination, 1, 1);
        layout.Controls.Add(browseButton, 2, 1);

        layout.Controls.Add(new Label { AutoSize = true }, 0, 2);
        layout.Controls.Add(chkHotToys, 1, 2);
        layout.SetColumnSpan(chkHotToys, 2);

        layout.Controls.Add(CreateLabel("Bestandsnaam:"), 0, 3);
        layout.Controls.Add(txtFileName, 1, 3);
        layout.SetColumnSpan(txtFileName, 2);

        layout.Controls.Add(CreateLabel("Startnummer:"), 0, 4);
        layout.Controls.Add(numStart, 1, 4);

        layout.Controls.Add(CreateLabel("Aantal cijfers:"), 0, 5);
        layout.Controls.Add(numDigits, 1, 5);

        layout.Controls.Add(CreateLabel("Voorbeeld:"), 0, 6);
        layout.Controls.Add(lblPreview, 1, 6);
        layout.SetColumnSpan(lblPreview, 2);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false
        };
        var okButton = new Button
        {
            Text = "Album analyseren",
            Width = 145,
            Height = 34
        };
        var cancelButton = new Button
        {
            Text = "Annuleren",
            Width = 105,
            Height = 34,
            DialogResult = DialogResult.Cancel
        };
        okButton.Click += (_, _) => Accept();
        buttons.Controls.Add(okButton);
        buttons.Controls.Add(cancelButton);

        layout.Controls.Add(buttons, 0, 7);
        layout.SetColumnSpan(buttons, 3);

        txtFileName.TextChanged += (_, _) => UpdatePreview();
        numStart.ValueChanged += (_, _) => UpdatePreview();
        numDigits.ValueChanged += (_, _) => UpdatePreview();
        chkHotToys.CheckedChanged += (_, _) =>
        {
            if (applyingPreset)
                return;

            if (chkHotToys.Checked)
            {
                applyingPreset = true;
                txtFileName.Text = FacebookFileNameFormatter.HotToysBloggerTemplate;
                applyingPreset = false;
            }

            UpdatePreview();
        };

        AcceptButton = okButton;
        CancelButton = cancelButton;
        Controls.Add(layout);
        UpdatePreview();
    }

    private static Label CreateLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(0, 7, 8, 7)
    };

    private void BrowseDestination()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Kies de doelmap voor het Facebook-album",
            InitialDirectory = Directory.Exists(txtDestination.Text.Trim())
                ? txtDestination.Text.Trim()
                : string.Empty
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            txtDestination.Text = dialog.SelectedPath;
    }

    private void Accept()
    {
        if (string.IsNullOrWhiteSpace(txtDestination.Text))
        {
            MessageBox.Show(
                "Kies eerst een doelmap.",
                "Facebook-album",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtFileName.Text))
        {
            MessageBox.Show(
                "Vul een bestandsnaam-template in.",
                "Facebook-album",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void UpdatePreview()
    {
        lblPreview.Text = FacebookFileNameFormatter.Format(
            txtFileName.Text,
            "715592432_1427899362707405_8122620896771832055_n.jpg",
            (int)numStart.Value,
            (int)numDigits.Value,
            chkHotToys.Checked);
    }
}
