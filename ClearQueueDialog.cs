namespace XiPHiAS.MediaFetch;

internal sealed class ClearQueueDialog : Form
{
    private readonly CheckBox completedCheckBox;
    private readonly CheckBox failedCheckBox;
    private readonly CheckBox allCheckBox;
    private readonly Label summaryLabel;
    private readonly Button clearButton;
    private readonly int completedCount;
    private readonly int failedCount;
    private readonly int totalCount;

    public bool RemoveCompleted => completedCheckBox.Checked;
    public bool RemoveFailed => failedCheckBox.Checked;
    public bool RemoveAll => allCheckBox.Checked;

    public ClearQueueDialog(
        int completedCount,
        int failedCount,
        int totalCount,
        bool removeCompleted,
        bool removeFailed,
        bool removeAll)
    {
        this.completedCount = completedCount;
        this.failedCount = failedCount;
        this.totalCount = totalCount;

        Text = "Downloadlijst leegmaken";
        ClientSize = new Size(520, 255);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;

        Controls.Add(new Label
        {
            Text = "Welke regels wil je uit de downloadlijst verwijderen?",
            Left = 20,
            Top = 20,
            Width = 480,
            Height = 24
        });

        completedCheckBox = new CheckBox
        {
            Text = $"Completed ({completedCount})",
            Left = 20,
            Top = 55,
            Width = 150,
            Checked = removeCompleted
        };

        failedCheckBox = new CheckBox
        {
            Text = $"Failed ({failedCount})",
            Left = 185,
            Top = 55,
            Width = 130,
            Checked = removeFailed
        };

        allCheckBox = new CheckBox
        {
            Text = $"Alles ({totalCount})",
            Left = 330,
            Top = 55,
            Width = 150,
            Checked = removeAll
        };

        summaryLabel = new Label
        {
            Left = 20,
            Top = 98,
            Width = 480,
            Height = 48
        };

        clearButton = new Button
        {
            Text = "Verwijderen",
            Left = 275,
            Top = 190,
            Width = 110,
            Height = 36,
            DialogResult = DialogResult.OK
        };

        var cancelButton = new Button
        {
            Text = "Annuleren",
            Left = 395,
            Top = 190,
            Width = 105,
            Height = 36,
            DialogResult = DialogResult.Cancel
        };

        completedCheckBox.CheckedChanged += (_, _) => UpdateSelection();
        failedCheckBox.CheckedChanged += (_, _) => UpdateSelection();
        allCheckBox.CheckedChanged += (_, _) => UpdateSelection();

        Controls.AddRange([
            completedCheckBox,
            failedCheckBox,
            allCheckBox,
            summaryLabel,
            clearButton,
            cancelButton
        ]);

        AcceptButton = clearButton;
        CancelButton = cancelButton;
        UpdateSelection();
    }

    private void UpdateSelection()
    {
        completedCheckBox.Enabled = !allCheckBox.Checked;
        failedCheckBox.Enabled = !allCheckBox.Checked;

        var removeCount = allCheckBox.Checked
            ? totalCount
            : (completedCheckBox.Checked ? completedCount : 0) +
              (failedCheckBox.Checked ? failedCount : 0);

        summaryLabel.Text = removeCount == 0
            ? "Er zijn met deze selectie geen regels om te verwijderen."
            : $"Er worden {removeCount} regel(s) uit de lijst verwijderd. " +
              "Gedownloade bestanden op schijf blijven behouden.";
        clearButton.Enabled = removeCount > 0;
    }
}
