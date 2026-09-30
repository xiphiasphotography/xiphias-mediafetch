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

            Met Clear verwijder je regels uit de downloadlijst. Voor het verwijderen verschijnt een venster waarin je Completed, Failed, Skipped of Alles kunt kiezen en het aantal te verwijderen regels ziet. Bestanden op schijf worden niet verwijderd.

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

        tabs.TabPages.Add(CreatePage("Facebook", """
            FACEBOOK-ALBUMS

            Plak één Facebook-album-URL met Ctrl+V in de wachtrij. MediaFetch herkent URL's in de vorm:

            https://www.facebook.com/media/set/?set=a....&type=3

            Eerst verschijnt een dialoog met de instellingen voor dat album.

            Doelmap
            Kies hier rechtstreeks waar de foto's van dit album moeten worden opgeslagen. Deze map blijft voor de Facebook-foto's behouden, ook wanneer de algemene instelling 'Doelmap per toevoegactie onthouden' uitstaat.

            Bestandsnaam
            De standaardtemplate is [N].[E].

            Beschikbare placeholders:
            [N]  originele Facebook-bestandsnaam zonder extensie
            [E]  extensie zonder punt
            [C]  oplopende counter in albumvolgorde
            [X]  waarde uit de ingebouwde Hot Toys Blogger-regex

            Startnummer staat bij ieder nieuw album standaard op 1. Met Aantal cijfers bepaal je de voorloopnullen. Bij 3 cijfers wordt de reeks bijvoorbeeld 001, 002, ... 010, ... 100.

            Voorbeelden:

            [C].[E]
            001.jpg
            002.jpg

            Hot Toys Blogger-foto's
            Dit vinkje vult automatisch de template in:

            Z[C]-FB_IMG_[X].[E]

            De Facebook-bestandsnaam:

            715592432_1427899362707405_8122620896771832055_n.jpg

            wordt dan voor de eerste foto:

            Z001-FB_IMG_8122620896771832055.jpg

            Als het bekende Facebook-patroon niet matcht, gebruikt [X] veilig dezelfde waarde als [N].

            Album analyseren
            Na de instellingen opent het Facebook-album in een afzonderlijke WebView2-browser. Meld je daar zo nodig aan bij Facebook en klik op Album analyseren. MediaFetch doorloopt eerst het album om de volgorde vast te leggen en opent daarna de foto's om de grootste geladen Facebook/CDN-afbeelding te kiezen.

            De Facebook-browsersessie wordt lokaal bewaard in een apart profiel onder:
            %LOCALAPPDATA%\XiPHiAS\MediaFetch\WebView2\Facebook

            MediaFetch bewaart je Facebook-wachtwoord niet in settings.json. De laatst gebruikte template, doelmap, Hot Toys-keuze en het aantal cijfers worden onthouden; het startnummer begint bij een nieuw album opnieuw op 1.

            Dit is een beta-functie. Facebook kan de webinterface wijzigen, waardoor albumherkenning of high-res-resolving later aangepast moet worden.
            """));

        tabs.TabPages.Add(CreatePage("YouTube", """
            YOUTUBE DOWNLOADEN

            Plak een normale YouTube-, youtu.be- of embed-URL in de wachtrij, of zet deze als volledige URL in een URL-bestand. yt-dlp en Deno halen bij Start de videotitel en beschikbare streams op; MediaFetch downloadt de gekozen streams vervolgens zelf.

            De videostream met de hoogste beschikbare resolutie en de audiostream met de hoogste bitrate worden afzonderlijk gedownload. Daarna voegt FFmpeg beide streams zonder hercodering samen in een MKV-bestand. Daardoor blijft de oorspronkelijke kwaliteit behouden.

            In de gekozen doelmap worden naast elkaar opgeslagen:

            Videotitel.video.webm   Originele videostream (extensie kan verschillen)
            Videotitel.audio.webm   Originele audiostream (extensie kan verschillen)
            Videotitel.mkv          Samengevoegde video met audio
            Videotitel.jpg          Hoogst beschikbare thumbnail

            De wachtrij toont afzonderlijk de voortgang van thumbnail, video, audio en samenvoegen. Samenvoegen gebeurt asynchroon; maximaal één FFmpeg-proces tegelijk voorkomt onnodige schijfbelasting. Andere downloads kunnen ondertussen doorgaan.

            Vereist zijn yt-dlp.exe, deno.exe en ffmpeg.exe. Plaats ze naast XiPHiAS.MediaFetch.exe, in de submap tools, of zorg dat ze via PATH beschikbaar zijn. Voor FFmpeg wordt ook de submap ffmpeg ondersteund. Gebruik de officiële yt-dlp.exe; deze bevat de EJS-challengescripts. Als samenvoegen mislukt, blijven de gedownloade bronbestanden behouden.

            Sommige video's werken alleen met een browsersessie. Ga dan naar Instellingen > YouTube-account en kies Aanmelden. MediaFetch probeert yt-dlp eerst zonder cookies. Alleen na een mislukking wordt voor de fallback tijdelijk een cookiesbestand gemaakt en direct daarna verwijderd.

            De sessie staat in een eigen profiel onder LocalAppData. MediaFetch leest of bewaart je wachtwoord niet en schrijft cookies niet naar settings.json of logbestanden. Met Afmelden worden de lokale sessiecookies gewist.

            Download alleen video's waarvoor je toestemming of downloadrechten hebt.
            """));

        tabs.TabPages.Add(CreatePage("Instellingen", """
            INSTELLINGEN

            Voltooide bestanden verwijderen bij toevoegen
            Als deze optie aanstaat, worden regels met de status Completed uit de wachtrij verwijderd zodra je een nieuwe URL-lijst toevoegt of URL's plakt. Bestanden op schijf worden niet verwijderd.

            Doelmap per toevoegactie onthouden
            Als deze optie aanstaat, bewaart ieder wachtrij-item zijn eigen doelmap. Dit maakt één downloadbatch naar meerdere mappen mogelijk. Padregels in geplakte tekst worden net als padregels in een URL-bestand verwerkt. Als de optie uitstaat, gebruikt de hele wachtrij bij Start de doelmap uit het hoofdscherm en zijn # padregels gewone opmerkingen.

            Clear-functionaliteit
            Kies welke opties standaard zijn aangevinkt wanneer je op Clear drukt: Completed, Failed, Skipped of Alles. Bij Alles worden de andere drie keuzes uitgeschakeld. In het bevestigingsvenster kun je de selectie voor die ene opruimactie nog aanpassen.

            Browserpreset en User-Agent
            Een preset vult een gangbare browser-User-Agent in. Je kunt deze daarna handmatig aanpassen. Dit kan helpen bij servers die verzoeken zonder herkenbare browsergegevens weigeren.

            Referer-URL
            Dit veld staat in het hoofdscherm en is geen permanente instelling. Vul het alleen in wanneer een server directe downloads weigert en een verwijzende webpagina verwacht.

            YouTube-account
            Aanmelden opent de officiële Google/YouTube-login in WebView2. Deze optionele sessie wordt alleen gebruikt als YouTube een anonieme streamaanvraag weigert. Afmelden wist de cookies uit het afzonderlijke MediaFetch-WebView2-profiel.
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

            Gedeeltelijke bestanden worden waar mogelijk hervat. Bij mislukte downloads probeert MediaFetch het verzoek maximaal drie keer opnieuw. Bestandsnaam, foutmelding en URL worden per doelmap in failed.log geschreven.

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
