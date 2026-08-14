namespace XiPHiAS.MediaFetch;

internal sealed class HelpDialog : Form
{
    public HelpDialog()
    {
        Text = "Handleiding - XiPHiAS MediaFetch";
        ClientSize = new Size(780, 590);
        MinimumSize = new Size(650, 480);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(14, 6)
        };

        tabs.TabPages.Add(CreatePage("Snel starten", """
            SNEL STARTEN

            1. Kies een URL-bestand, sleep een URL-bestand naar het venster of plak URL's met Ctrl+V in de wachtrij.
            2. Kies de doelmap in het hoofdscherm.
            3. Vul alleen indien nodig een Referer-URL in.
            4. Kies hoeveel downloads tegelijk mogen lopen.
            5. Controleer de kolom Doelmap en klik op Start.

            Bij bestaande bestanden kun je kiezen voor overschrijven, overslaan of hernoemen. Met Stop worden geen nieuwe downloads gestart; actieve downloads mogen eerst netjes eindigen.

            Met Clear verwijder je regels uit de downloadlijst. Voor het verwijderen verschijnt een venster waarin je Completed, Failed of Alles kunt kiezen en het aantal te verwijderen regels ziet. Bestanden op schijf worden niet verwijderd.

            Alleen volledige http- en https-URL's worden toegevoegd. Lege regels en ongeldige regels worden overgeslagen.
            """));

        tabs.TabPages.Add(CreatePage("Meerdere doelmappen", """
            MEERDERE DOELMAPPEN

            Zet in Instellingen de optie 'Doelmap per toevoegactie onthouden' aan.

            URL-bestand
            De doelmap uit het hoofdscherm geldt als standaard. Je kunt in het bestand van doelmap wisselen met een volledig pad achter #. Alle volgende URL's gebruiken dat pad tot de volgende padregel.

            Voorbeeld:

            # D:\Pictures\Camera
            https://example.com/camera-1.jpg
            https://example.com/camera-2.jpg
            # C:\Temp
            https://example.com/preview.jpg

            URL's vóór de eerste padregel gebruiken de doelmap uit het hoofdscherm. Een # regel die geen volledig pad bevat, blijft een gewone opmerking.

            Plakken
            Plak je tekst met volledige paden achter #, dan worden die paden op dezelfde manier verwerkt als in een URL-bestand en verschijnt geen mapkeuze. Zonder zulke padregels verschijnt bij Ctrl+V een mapkeuze. De map uit het hoofdscherm is voorgeselecteerd en geldt dan voor alle URL's uit die plakactie.

            Dezelfde URL mag meerdere keren in de wachtrij staan wanneer de doelmap verschilt.
            """));

        tabs.TabPages.Add(CreatePage("Instellingen", """
            INSTELLINGEN

            Voltooide bestanden verwijderen bij toevoegen
            Als deze optie aanstaat, worden regels met de status Completed uit de wachtrij verwijderd zodra je een nieuwe URL-lijst toevoegt of URL's plakt. Bestanden op schijf worden niet verwijderd.

            Doelmap per toevoegactie onthouden
            Als deze optie aanstaat, bewaart ieder wachtrij-item zijn eigen doelmap. Dit maakt één downloadbatch naar meerdere mappen mogelijk. Padregels in geplakte tekst worden net als padregels in een URL-bestand verwerkt. Als de optie uitstaat, gebruikt de hele wachtrij bij Start de doelmap uit het hoofdscherm en zijn # padregels gewone opmerkingen.

            Clear-functionaliteit
            Kies welke opties standaard zijn aangevinkt wanneer je op Clear drukt: Completed, Failed of Alles. Bij Alles worden de andere twee keuzes uitgeschakeld. In het bevestigingsvenster kun je de selectie voor die ene opruimactie nog aanpassen.

            Browserpreset en User-Agent
            Een preset vult een gangbare browser-User-Agent in. Je kunt deze daarna handmatig aanpassen. Dit kan helpen bij servers die verzoeken zonder herkenbare browsergegevens weigeren.

            Referer-URL
            Dit veld staat in het hoofdscherm en is geen permanente instelling. Vul het alleen in wanneer een server directe downloads weigert en een verwijzende webpagina verwacht.
            """));

        tabs.TabPages.Add(CreatePage("Status en bestanden", """
            STATUS EN BESTANDEN

            Waiting       Wacht om gestart te worden.
            Downloading   Wordt momenteel gedownload.
            Completed     Is succesvol opgeslagen.
            Skipped       Is overgeslagen vanwege een bestaand bestand.

            De bestandsnaam wordt afgeleid uit de URL. Als de URL geen bruikbare naam bevat, wordt automatisch een unieke naam gemaakt.

            In de wachtrij worden de bestandsnaam en laatste doelmap compact weergegeven. Beweeg de muis over de bestandsnaam voor de volledige URL of over de doelmap voor het volledige doelpad.

            Dubbelklik op een bestandsnaam om de URL in de standaardbrowser te openen. Dubbelklik op een doelmap om deze in Windows Verkenner te openen. Dezelfde acties staan in het rechtermuisknopmenu. Via ✕ Verwijderen kun je een afzonderlijke regel uit de lijst halen.

            Gedeeltelijke bestanden worden waar mogelijk hervat. Bij mislukte downloads probeert MediaFetch het verzoek maximaal drie keer opnieuw. Mislukte URL's worden per doelmap in failed.txt geschreven.

            Een ontbrekende doelmap wordt bij Start gemeld en kan met jouw bevestiging worden aangemaakt.
            """));

        var closeButton = new Button
        {
            Text = "Sluiten",
            DialogResult = DialogResult.OK,
            Width = 110,
            Height = 36,
            Anchor = AnchorStyles.Right
        };

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 58,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(10),
            WrapContents = false
        };
        buttonPanel.Controls.Add(closeButton);

        AcceptButton = closeButton;
        CancelButton = closeButton;
        Controls.Add(tabs);
        Controls.Add(buttonPanel);
    }

    private static TabPage CreatePage(string title, string text)
    {
        var page = new TabPage(title) { Padding = new Padding(10) };
        var content = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = SystemColors.Window,
            Font = new Font("Segoe UI", 10),
            Text = text.Trim(),
            DetectUrls = false,
            TabStop = false
        };
        page.Controls.Add(content);
        return page;
    }
}
