using System.Diagnostics;

namespace XiPHiAS.MediaFetch;

internal sealed class AboutDialog : Form
{
    private readonly Image logoImage;

    public AboutDialog()
    {
        Text = "Over XiPHiAS MediaFetch";
        ClientSize = new Size(640, 430);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;

        logoImage = Branding.LoadLogo();

        var logo = new PictureBox
        {
            Image = logoImage,
            SizeMode = PictureBoxSizeMode.Zoom,
            Left = 60,
            Top = 25,
            Width = 520,
            Height = 240
        };

        var version = new Label
        {
            Text = $"Versie {Application.ProductVersion}",
            AutoSize = true,
            Left = 60,
            Top = 280,
            Font = new Font(Font, FontStyle.Bold)
        };

        var detailsLayout = new TableLayoutPanel
        {
            AutoSize = true,
            Left = 60,
            Top = 310,
            Width = 520,
            ColumnCount = 2,
            RowCount = 2,
            Margin = Padding.Empty
        };
        detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var description = new Label
        {
            Text = "Een lichte Windows-app voor het gelijktijdig downloaden van media-URL’s.",
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            Margin = new Padding(0, 0, 0, 18)
        };

        var githubLabel = new Label
        {
            Text = "Project:",
            AutoSize = true,
            Margin = new Padding(0, 0, 8, 0)
        };

        var githubLink = new LinkLabel
        {
            Text = Branding.GitHubUrl,
            AutoSize = true,
            Margin = Padding.Empty
        };
        githubLink.LinkClicked += (_, _) => Process.Start(new ProcessStartInfo
        {
            FileName = Branding.GitHubUrl,
            UseShellExecute = true
        });

        detailsLayout.Controls.Add(description, 0, 0);
        detailsLayout.SetColumnSpan(description, 2);
        detailsLayout.Controls.Add(githubLabel, 0, 1);
        detailsLayout.Controls.Add(githubLink, 1, 1);

        var closeButton = new Button
        {
            Text = "Sluiten",
            Width = 100,
            Height = 34,
            Left = 480,
            Top = 382,
            DialogResult = DialogResult.OK
        };

        AcceptButton = closeButton;
        CancelButton = closeButton;
        Controls.AddRange([
            logo,
            version,
            detailsLayout,
            closeButton
        ]);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            logoImage.Dispose();
        }

        base.Dispose(disposing);
    }
}
