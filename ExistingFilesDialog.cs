namespace XiPHiAS.MediaFetch;

internal enum ExistingFileAction
{
    Overwrite,
    Skip,
    Rename
}

internal sealed class ExistingFilesDialog : Form
{
    public ExistingFileAction? SelectedAction { get; private set; }

    public ExistingFilesDialog(
        int existingFileCount,
        int completeFileCount)
    {
        Text = "Bestaande bestanden";
        ClientSize = new Size(520, 190);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;

        var message = new Label
        {
            AutoSize = false,
            Left = 20,
            Top = 20,
            Width = 480,
            Height = 80,
            Text = BuildMessage(existingFileCount, completeFileCount)
        };

        var overwriteButton = CreateButton("Overschrijven", 20, ExistingFileAction.Overwrite);
        var skipButton = CreateButton("Overslaan", 180, ExistingFileAction.Skip);
        var renameButton = CreateButton("Hernoemen", 340, ExistingFileAction.Rename);

        Controls.AddRange([message, overwriteButton, skipButton, renameButton]);
    }

    private static string BuildMessage(
        int existingFileCount,
        int completeFileCount)
    {
        var message = existingFileCount == 1
            ? "Er bestaat al 1 doelbestand."
            : $"Er bestaan al {existingFileCount} doelbestanden.";

        if (completeFileCount > 0)
        {
            message += $" Daarvan {(completeFileCount == 1 ? "is er 1 volledig" : $"zijn er {completeFileCount} volledig")} op basis van de bestandsgrootte.";
        }

        return message + " Wat wil je hiermee doen?";
    }

    private Button CreateButton(
        string text,
        int left,
        ExistingFileAction action)
    {
        var button = new Button
        {
            Text = text,
            Left = left,
            Top = 125,
            Width = 140,
            Height = 38
        };

        button.Click += (_, _) =>
        {
            SelectedAction = action;
            DialogResult = DialogResult.OK;
            Close();
        };

        return button;
    }
}
