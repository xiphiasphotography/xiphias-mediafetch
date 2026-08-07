using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;

namespace XiPHiAS.MediaFetch;

public class MainForm : Form
{
    private static readonly Regex AbsoluteUrlRegex = new(
        @"https?://[^\s\""'<>]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private readonly AppSettings settings;
    private readonly Image headerLogoImage;

    private readonly TextBox txtFile;
    private readonly TextBox txtDestination;
    private readonly TextBox txtReferer;

    private readonly Button btnSelectFile;
    private readonly Button btnSelectDestination;
    private readonly Button btnStart;

    private readonly NumericUpDown numConcurrent;

    private readonly ProgressBar progressTotal;
    private readonly Label lblSummary;

    private readonly ListView listDownloads;

    private CancellationTokenSource? cancellationTokenSource;
    private bool isDownloading;

    public MainForm()
    {
        settings = AppSettings.Load();

        Text = "XiPHiAS MediaFetch";
        MinimumSize = new Size(820, 560);
        ClientSize = new Size(1080, 700);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;

        Icon = System.Drawing.Icon.ExtractAssociatedIcon(
            Application.ExecutablePath
        );

        AllowDrop = true;

        var menuStrip = new MenuStrip();
        var settingsMenuItem = new ToolStripMenuItem("Instellingen");
        settingsMenuItem.Click += SettingsMenuItem_Click;
        menuStrip.Items.Add(settingsMenuItem);

        var helpMenuItem = new ToolStripMenuItem("Help");
        var aboutMenuItem = new ToolStripMenuItem("Over XiPHiAS MediaFetch");
        aboutMenuItem.Click += (_, _) =>
        {
            using var dialog = new AboutDialog();
            dialog.ShowDialog(this);
        };
        helpMenuItem.DropDownItems.Add(aboutMenuItem);
        menuStrip.Items.Add(helpMenuItem);
        MainMenuStrip = menuStrip;

        DragEnter += MainForm_DragEnter;
        DragDrop += MainForm_DragDrop;

        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 1,
            RowCount = 6
        };
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        headerLogoImage = Branding.LoadLogo();

        var headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(12, 6, 16, 6)
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var headerLogo = new PictureBox
        {
            Image = headerLogoImage,
            SizeMode = PictureBoxSizeMode.Zoom,
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };

        var headerInfo = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Margin = new Padding(18, 0, 0, 0)
        };
        headerInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        headerInfo.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerInfo.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        headerInfo.Controls.Add(new Label
        {
            Text = "Download afbeeldingen en video’s vanuit een URL-lijst.",
            AutoSize = true,
            ForeColor = Color.FromArgb(25, 91, 113),
            Margin = Padding.Empty
        }, 0, 1);
        headerInfo.Controls.Add(new Label
        {
            Text = $"Versie {Application.ProductVersion}",
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(0, 5, 0, 0)
        }, 0, 2);
        headerLayout.Controls.Add(headerLogo, 0, 0);
        headerLayout.Controls.Add(headerInfo, 1, 0);
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var inputLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 3,
            RowCount = 3,
            Margin = new Padding(0, 0, 0, 10)
        };
        inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 165));
        inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));

        var lblFile = CreateFieldLabel("URL-bestand:");

        txtFile = new TextBox
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(3, 5, 8, 5)
        };

        btnSelectFile = new Button
        {
            Text = "…",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 2, 0, 2)
        };

        btnSelectFile.Click += BtnSelectFile_Click;
        txtFile.TextChanged += (_, _) => UpdateActionButtonState();

        var lblDestination = CreateFieldLabel("Doelmap:");

        txtDestination = new TextBox
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(3, 5, 8, 5),
            Text = GetInitialDestinationDirectory()
        };

        btnSelectDestination = new Button
        {
            Text = "…",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 2, 0, 2)
        };

        btnSelectDestination.Click +=
            BtnSelectDestination_Click;
        txtDestination.TextChanged += (_, _) => UpdateActionButtonState();

        var lblReferer = CreateFieldLabel("Referer URL (optioneel):");

        txtReferer = new TextBox
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(3, 5, 8, 5)
        };

        inputLayout.Controls.Add(lblFile, 0, 0);
        inputLayout.Controls.Add(txtFile, 1, 0);
        inputLayout.Controls.Add(btnSelectFile, 2, 0);
        inputLayout.Controls.Add(lblDestination, 0, 1);
        inputLayout.Controls.Add(txtDestination, 1, 1);
        inputLayout.Controls.Add(btnSelectDestination, 2, 1);
        inputLayout.Controls.Add(lblReferer, 0, 2);
        inputLayout.Controls.Add(txtReferer, 1, 2);
        inputLayout.SetColumnSpan(txtReferer, 2);

        var actionLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 10)
        };
        actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var lblConcurrent = CreateFieldLabel("Tegelijk:");
        lblConcurrent.AutoSize = true;
        lblConcurrent.Margin = new Padding(0, 8, 6, 0);

        numConcurrent = new NumericUpDown
        {
            Width = 70,
            Minimum = 1,
            Maximum = 20,
            Value = 4,
            Margin = new Padding(0, 3, 0, 3)
        };

        btnStart = new Button
        {
            Text = "Start",
            Width = 100,
            Height = 32,
            Enabled = false,
            BackColor = Color.SeaGreen,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            Margin = new Padding(0)
        };

        btnStart.Click += BtnStart_Click;
        actionLayout.Controls.Add(lblConcurrent, 0, 0);
        actionLayout.Controls.Add(numConcurrent, 1, 0);
        actionLayout.Controls.Add(btnStart, 3, 0);

        progressTotal = new ProgressBar
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8)
        };

        listDownloads = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            HideSelection = false,
            Margin = new Padding(0)
        };

        listDownloads.Columns.Add("Bestand", 300);
        listDownloads.Columns.Add("Voortgang", 90);
        listDownloads.Columns.Add("Grootte", 100);
        listDownloads.Columns.Add("Snelheid", 110);
        listDownloads.Columns.Add("ETA", 80);
        listDownloads.Columns.Add("Status", 250);
        listDownloads.Resize += (_, _) => ResizeDownloadColumns();

        lblSummary = new Label
        {
            Text = "Gereed om te downloaden",
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 0)
        };

        rootLayout.Controls.Add(headerLayout, 0, 0);
        rootLayout.Controls.Add(inputLayout, 0, 1);
        rootLayout.Controls.Add(actionLayout, 0, 2);
        rootLayout.Controls.Add(progressTotal, 0, 3);
        rootLayout.Controls.Add(listDownloads, 0, 4);
        rootLayout.Controls.Add(lblSummary, 0, 5);

        Controls.Add(rootLayout);
        Controls.Add(menuStrip);
        menuStrip.Dock = DockStyle.Top;
        ResizeDownloadColumns();
        UpdateActionButtonState();
    }

    private static Label CreateFieldLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(0, 8, 8, 8)
    };

    private void ResizeDownloadColumns()
    {
        if (listDownloads.Columns.Count != 6 || listDownloads.ClientSize.Width <= 0)
        {
            return;
        }

        var fixedWidth = 90 + 100 + 110 + 80 + 250;
        listDownloads.Columns[0].Width = Math.Max(
            220,
            listDownloads.ClientSize.Width - fixedWidth - 8
        );
    }

    private void BtnSelectFile_Click(
        object? sender,
        EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter =
                "URL- en HTML-bestanden (*.txt;*.html;*.htm)|*.txt;*.html;*.htm|" +
                "Tekstbestanden (*.txt)|*.txt|" +
                "HTML-bestanden (*.html;*.htm)|*.html;*.htm|" +
                "Alle bestanden (*.*)|*.*"
        };

        if (Directory.Exists(settings.LastSourceDirectory))
        {
            dialog.InitialDirectory = settings.LastSourceDirectory;
        }

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            txtFile.Text = dialog.FileName;
            RememberSourceDirectory(dialog.FileName);
        }
    }

    private void BtnSelectDestination_Click(
        object? sender,
        EventArgs e)
    {
        var initialDirectory = Directory.Exists(txtDestination.Text)
            ? txtDestination.Text
            : settings.LastDestinationDirectory;

        using var dialog = new FolderBrowserDialog
        {
            InitialDirectory = Directory.Exists(initialDirectory)
                ? initialDirectory
                : string.Empty
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            txtDestination.Text =
                dialog.SelectedPath;

            settings.LastDestinationDirectory =
                dialog.SelectedPath;
            settings.Save();
        }
    }

    private async void BtnStart_Click(
        object? sender,
        EventArgs e)
    {
        if (isDownloading)
        {
            btnStart.Enabled = false;
            btnStart.Text = "Stoppen…";
            UpdateActionButtonAppearance();
            lblSummary.Text = "Wachten tot actieve downloads klaar zijn…";
            cancellationTokenSource?.Cancel();
            return;
        }

        if (!File.Exists(txtFile.Text))
        {
            MessageBox.Show(
                "Selecteer eerst een geldig URL-bestand.",
                "XiPHiAS MediaFetch",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(
            txtDestination.Text))
        {
            MessageBox.Show(
                "Selecteer een doelmap.",
                "XiPHiAS MediaFetch",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );

            return;
        }

        Uri? referer = null;

        if (
            !string.IsNullOrWhiteSpace(txtReferer.Text) &&
            (!Uri.TryCreate(txtReferer.Text.Trim(), UriKind.Absolute, out referer) ||
             (referer.Scheme != Uri.UriSchemeHttp &&
              referer.Scheme != Uri.UriSchemeHttps))
        )
        {
            MessageBox.Show(
                "Voer een geldige http- of https-URL in als referer.",
                "XiPHiAS MediaFetch",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            txtReferer.Focus();
            txtReferer.SelectAll();
            return;
        }

        var urls = ReadUrlsFromInputFile(txtFile.Text)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (urls.Count == 0)
        {
            MessageBox.Show(
                "Het bestand bevat geen URL's.",
                "XiPHiAS MediaFetch",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            return;
        }

        Directory.CreateDirectory(
            txtDestination.Text
        );

        listDownloads.Items.Clear();

        using var downloader = new Downloader(settings.UserAgent, referer);

        var items = urls
            .Select(CreateDownloadItem)
            .ToList();

        var existingItems = items
            .Where(item => File.Exists(item.DestinationPath))
            .ToList();

        if (existingItems.Count > 0)
        {
            UseWaitCursor = true;
            btnStart.Enabled = false;
            lblSummary.Text = "Bestaande bestanden controleren…";

            try
            {
                await DetectCompleteFilesAsync(downloader, existingItems);
            }
            finally
            {
                UseWaitCursor = false;
                UpdateActionButtonState();
            }
        }

        ExistingFileAction? existingFileAction = null;

        if (existingItems.Count > 0)
        {
            using var existingFilesDialog =
                new ExistingFilesDialog(
                    existingItems.Count,
                    existingItems.Count(item => item.ExistingFileIsComplete)
                );

            if (existingFilesDialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            existingFileAction = existingFilesDialog.SelectedAction;

            if (existingFileAction == ExistingFileAction.Overwrite)
            {
                foreach (var item in existingItems)
                {
                    item.OverwriteExisting = true;
                }
            }
            else if (existingFileAction == ExistingFileAction.Rename)
            {
                RenameExistingItems(items, existingItems);
            }
        }

        foreach (var item in items)
        {
            var row = new ListViewItem(
                item.FileName
            );

            row.SubItems.Add("0%");
            row.SubItems.Add(
                item.TotalBytes.HasValue
                    ? FormatBytes(item.TotalBytes.Value)
                    : "—"
            );
            row.SubItems.Add("—");
            row.SubItems.Add("—");
            row.SubItems.Add(
                existingFileAction == ExistingFileAction.Skip &&
                File.Exists(item.DestinationPath)
                    ? "Skipped"
                    : "Waiting"
            );
            row.Tag = item;

            listDownloads.Items.Add(row);
        }

        progressTotal.Minimum = 0;
        progressTotal.Maximum = items.Count;
        progressTotal.Value = 0;

        SetDownloadingState(true);
        lblSummary.Text = $"0 van {items.Count} verwerkt";

        cancellationTokenSource =
            new CancellationTokenSource();

        using var semaphore = new SemaphoreSlim(
            (int)numConcurrent.Value
        );

        var failed =
            new ConcurrentBag<string>();

        var skipped = existingFileAction == ExistingFileAction.Skip
            ? existingItems.Count
            : 0;

        var downloaded = 0;

        var completed = 0;

        try
        {
            var tasks = items.Select(
                async (item, index) =>
                {
                    if (
                        existingFileAction == ExistingFileAction.Skip &&
                        File.Exists(item.DestinationPath)
                    )
                    {
                        Interlocked.Increment(ref completed);

                        Invoke((Action)(() =>
                        {
                            progressTotal.Value = Math.Min(
                                completed,
                                progressTotal.Maximum
                            );
                            lblSummary.Text = $"{completed} van {items.Count} verwerkt";
                        }));

                        return;
                    }

                    await semaphore.WaitAsync(
                        cancellationTokenSource.Token
                    );

                    try
                    {
                        var progress =
                            new Progress<DownloadItem>(
                                current =>
                                {
                                    if (
                                        index < 0 ||
                                        index >= listDownloads.Items.Count
                                    )
                                    {
                                        return;
                                    }

                                    var row =
                                        listDownloads.Items[index];

                                    row.SubItems[1].Text =
                                        current.TotalBytes.HasValue
                                            ? $"{current.Progress}%"
                                            : FormatBytes(
                                                current.BytesDownloaded
                                            );

                                    row.SubItems[2].Text =
                                        current.TotalBytes.HasValue
                                            ? FormatBytes(current.TotalBytes.Value)
                                            : "Onbekend";

                                    row.SubItems[3].Text =
                                        current.BytesPerSecond > 0
                                            ? $"{FormatBytes((long)current.BytesPerSecond)}/s"
                                            : "—";

                                    row.SubItems[4].Text =
                                        FormatEta(current.EstimatedTimeRemaining);

                                    row.SubItems[5].Text =
                                        current.Status;
                                });

                        await downloader.DownloadAsync(
                            item,
                            progress,
                            CancellationToken.None
                        );

                        Interlocked.Increment(ref downloaded);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch
                    {
                        failed.Add(item.Url);
                    }
                    finally
                    {
                        semaphore.Release();

                        Interlocked.Increment(
                            ref completed
                        );

                        Invoke((Action)(() =>
                        {
                            progressTotal.Value =
                                Math.Min(
                                    completed,
                                    progressTotal.Maximum
                                );
                            lblSummary.Text = $"{completed} van {items.Count} verwerkt";
                        }));
                    }
                });

            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // De gebruiker heeft de download gestopt.
        }
        finally
        {
            SetDownloadingState(false);
            lblSummary.Text = $"Gedownload: {downloaded}   Overgeslagen: {skipped}   Fouten: {failed.Count}";
        }

        if (!failed.IsEmpty)
        {
            var failedFile = Path.Combine(
                txtDestination.Text,
                "failed.txt"
            );

            await File.WriteAllLinesAsync(
                failedFile,
                failed
            );
        }

        if (
            cancellationTokenSource
                .IsCancellationRequested)
        {
            MessageBox.Show(
                $"Download gestopt.\n\nGedownload: {downloaded}\nOvergeslagen: {skipped}\nFouten: {failed.Count}",
                "XiPHiAS MediaFetch",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        else
        {
            MessageBox.Show(
                $"Download voltooid.\n\nGedownload: {downloaded}\nOvergeslagen: {skipped}\nFouten: {failed.Count}" +
                (failed.IsEmpty
                    ? string.Empty
                    : "\n\nEen referer-URL invullen kan helpen wanneer de server downloads weigert."),
                "XiPHiAS MediaFetch",
                MessageBoxButtons.OK,
                failed.IsEmpty
                    ? MessageBoxIcon.Information
                    : MessageBoxIcon.Warning
            );

            if (!failed.IsEmpty)
            {
                txtReferer.Focus();
                txtReferer.SelectAll();
            }
        }
    }

    private static IEnumerable<string> ReadUrlsFromInputFile(string path)
    {
        var extension = Path.GetExtension(path);

        if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".htm", StringComparison.OrdinalIgnoreCase))
        {
            var html = File.ReadAllText(path);

            return AbsoluteUrlRegex
                .Matches(html)
                .Select(match => WebUtility.HtmlDecode(match.Value)
                    .TrimEnd('"', '\'', ',', '.', ';', ')', ']', '}'))
                .Where(url =>
                    Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp ||
                     uri.Scheme == Uri.UriSchemeHttps));
        }

        return File.ReadLines(path)
            .Select(line => line.Trim())
            .Where(line =>
                !string.IsNullOrWhiteSpace(line) &&
                !line.StartsWith('#'));
    }

    private DownloadItem CreateDownloadItem(
        string url)
    {
        var uri = new Uri(url);

        var fileName =
            Path.GetFileName(uri.LocalPath);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName =
                $"download_{Guid.NewGuid():N}";
        }

        fileName = Uri.UnescapeDataString(
            fileName
        );

        foreach (
            var invalidChar
            in Path.GetInvalidFileNameChars())
        {
            fileName =
                fileName.Replace(
                    invalidChar,
                    '_'
                );
        }

        return new DownloadItem
        {
            Url = url,
            FileName = fileName,
            DestinationPath = Path.Combine(
                txtDestination.Text,
                fileName
            )
        };
    }

    private static void RenameExistingItems(
        IReadOnlyCollection<DownloadItem> allItems,
        IReadOnlyCollection<DownloadItem> existingItems)
    {
        var reservedPaths = new HashSet<string>(
            allItems
                .Except(existingItems)
                .Select(item => item.DestinationPath),
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var item in existingItems)
        {
            var directory = Path.GetDirectoryName(item.DestinationPath)
                ?? string.Empty;
            var baseName = Path.GetFileNameWithoutExtension(item.FileName);
            var extension = Path.GetExtension(item.FileName);
            var number = 2;
            string renamedPath;

            do
            {
                item.FileName = $"{baseName} ({number}){extension}";
                renamedPath = Path.Combine(directory, item.FileName);
                number++;
            }
            while (File.Exists(renamedPath) || reservedPaths.Contains(renamedPath));

            item.DestinationPath = renamedPath;
            reservedPaths.Add(renamedPath);
        }
    }

    private static string FormatBytes(
        long bytes)
    {
        string[] units =
            ["B", "KB", "MB", "GB", "TB"];

        double size = bytes;
        var unit = 0;

        while (
            size >= 1024 &&
            unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.##} {units[unit]}";
    }

    private static string FormatEta(TimeSpan? eta)
    {
        if (!eta.HasValue)
        {
            return "—";
        }

        if (eta.Value <= TimeSpan.Zero)
        {
            return "00:00";
        }

        return eta.Value.TotalHours >= 1
            ? eta.Value.ToString(@"hh\:mm\:ss")
            : eta.Value.ToString(@"mm\:ss");
    }

    private static async Task DetectCompleteFilesAsync(
        Downloader downloader,
        IReadOnlyCollection<DownloadItem> existingItems)
    {
        using var semaphore = new SemaphoreSlim(6);

        await Task.WhenAll(existingItems.Select(async item =>
        {
            await semaphore.WaitAsync();

            try
            {
                var remoteSize = await downloader.GetRemoteSizeAsync(item.Url);
                item.TotalBytes = remoteSize;

                if (remoteSize.HasValue)
                {
                    item.ExistingFileIsComplete =
                        new FileInfo(item.DestinationPath).Length == remoteSize.Value;
                }
            }
            finally
            {
                semaphore.Release();
            }
        }));
    }

    private void MainForm_DragEnter(
        object? sender,
        DragEventArgs e)
    {
        if (
            e.Data?.GetDataPresent(
                DataFormats.FileDrop
            ) == true)
        {
            e.Effect =
                DragDropEffects.Copy;
        }
    }

    private void MainForm_DragDrop(
        object? sender,
        DragEventArgs e)
    {
        if (
            e.Data?.GetData(
                DataFormats.FileDrop
            ) is string[] files &&
            files.Length > 0)
        {
            txtFile.Text =
                files[0];

            RememberSourceDirectory(files[0]);
        }
    }

    private string GetInitialDestinationDirectory()
    {
        if (Directory.Exists(settings.LastDestinationDirectory))
        {
            return settings.LastDestinationDirectory;
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads"
        );
    }

    private void RememberSourceDirectory(string fileName)
    {
        var directory = Path.GetDirectoryName(fileName);

        if (Directory.Exists(directory))
        {
            settings.LastSourceDirectory = directory;
            settings.Save();
        }
    }

    private void SettingsMenuItem_Click(object? sender, EventArgs e)
    {
        using var dialog = new SettingsDialog(settings.UserAgent);

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(dialog.UserAgent))
        {
            MessageBox.Show(
                "De User-Agent mag niet leeg zijn.",
                "XiPHiAS MediaFetch",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return;
        }

        try
        {
            using var request = new HttpRequestMessage();
            request.Headers.UserAgent.ParseAdd(dialog.UserAgent);
        }
        catch (FormatException)
        {
            MessageBox.Show(
                "De opgegeven User-Agent heeft geen geldige indeling.",
                "XiPHiAS MediaFetch",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return;
        }

        settings.UserAgent = dialog.UserAgent;
        settings.Save();
    }

    private void SetDownloadingState(bool downloading)
    {
        isDownloading = downloading;

        btnStart.Text = downloading ? "Stop" : "Start";
        btnStart.BackColor = downloading ? Color.Firebrick : Color.SeaGreen;

        txtFile.Enabled = !downloading;
        txtDestination.Enabled = !downloading;
        txtReferer.Enabled = !downloading;
        btnSelectFile.Enabled = !downloading;
        btnSelectDestination.Enabled = !downloading;
        numConcurrent.Enabled = !downloading;

        if (downloading)
        {
            btnStart.Enabled = true;
            UpdateActionButtonAppearance();
        }
        else
        {
            UpdateActionButtonState();
        }
    }

    private void UpdateActionButtonState()
    {
        if (isDownloading)
        {
            return;
        }

        btnStart.Enabled =
            File.Exists(txtFile.Text.Trim()) &&
            !string.IsNullOrWhiteSpace(txtDestination.Text);

        UpdateActionButtonAppearance();
    }

    private void UpdateActionButtonAppearance()
    {
        if (!btnStart.Enabled)
        {
            btnStart.BackColor = SystemColors.Control;
            btnStart.ForeColor = SystemColors.GrayText;
            btnStart.FlatAppearance.BorderColor = SystemColors.ControlDark;
            return;
        }

        btnStart.BackColor = isDownloading
            ? Color.Firebrick
            : Color.SeaGreen;
        btnStart.ForeColor = Color.White;
        btnStart.FlatAppearance.BorderColor = btnStart.BackColor;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            headerLogoImage.Dispose();
            cancellationTokenSource?.Dispose();
        }

        base.Dispose(disposing);
    }
}
