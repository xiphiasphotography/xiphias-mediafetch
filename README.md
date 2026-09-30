# XiPHiAS MediaFetch

XiPHiAS MediaFetch is een lichte Windows-app voor het gelijktijdig downloaden van afbeeldingen, video's en andere mediabestanden uit een URL-lijst. De app is gebouwd met C# en .NET 8 WinForms.

## Functies

- Lees URL's uit een tekstbestand (`.txt`), één URL per regel.
- Plak een gekopieerde lijst met URL's met `Ctrl+V` rechtstreeks in de wachtrij.
- Onthoud optioneel per toevoegactie een eigen doelmap, zodat één wachtrij naar meerdere mappen kan downloaden.
- Verwijder optioneel voltooide downloads zodra nieuwe URL's worden toegevoegd.
- Negeer lege regels, ongeldige regels en `#`-commentaarregels die geen volledig doelpad bevatten.
- Download 1 tot 20 bestanden tegelijk (standaard: 4).
- Herken YouTube-, youtu.be- en YouTube-embed-URL's automatisch.
- Download voor YouTube de hoogste beschikbare videoresolutie, de audio met de hoogste bitrate en de thumbnail met de hoogste resolutie.
- Bewaar de oorspronkelijke video- en audiostream en voeg deze zonder kwaliteitsverlies asynchroon samen als MKV.
- Toon de voortgang en resterende tijd van zowel de streamdownloads als het samenvoegen.
- Meld optioneel via een afgeschermd WebView2-profiel aan bij YouTube voor streams die anoniem worden geweigerd.
- Hervat gedeeltelijke downloads als de server HTTP-rangeverzoeken ondersteunt.
- Volg redirects en pak gzip-, deflate- en Brotli-responses uit.
- Probeer een mislukte download maximaal drie keer, met oplopende wachttijd.
- Corrigeer na een mislukte eerste poging HTML-entiteiten en onjuiste URL-padencoding automatisch (bijvoorbeeld `&amp;` naar `%26`).
- Toon per bestand de voortgang, grootte, snelheid, resterende tijd en status.
- Bekijk compacte bestands- en doelmapnamen in de wachtrij en beweeg erover voor de volledige URL of het volledige doelpad.
- Open met dubbelklik of het rechtermuisknopmenu een URL in de standaardbrowser of een doelmap in Windows Verkenner.
- Verwijder een afzonderlijke wachtrijregel via **✕ Verwijderen** in het rechtermuisknopmenu.
- Schakel **Start** uit wanneer de wachtrij leeg is of alle items zijn voltooid of overgeslagen.
- Verwijder via **Clear** bevestigde categorieën uit de wachtrij zonder bestanden op schijf te verwijderen.
- Stop een actieve downloadbatch vanuit de interface.
- Controleer bestaande bestanden aan de hand van de externe `Content-Length`, indien beschikbaar.
- Overschrijf, sla over of hernoem bestaande doelbestanden automatisch.
- Stel optioneel een Referer-URL in voor servers die directe downloads weigeren.
- Kies een browser-User-Agent-preset of vul een eigen User-Agent in.
- Vermijd WebP-responses, tenzij de gevraagde URL expliciet op `.webp` eindigt.
- Sleep een URL-bestand naar het hoofdvenster.
- Schrijf bestandsnaam, foutmelding en URL van mislukte downloads naar `failed.log` in de doelmap.
- Onthoud de laatst gebruikte bron- en doelmap.
- Onthoud de vensterpositie, venstergrootte en maximalisatiestatus.
- Controleer een ingevoerde doelmap direct en bied aan een ontbrekende map aan te maken.
- Open via **Help > Handleiding** uitleg over de workflow, meerdere doelmappen, instellingen en statussen.

## Vereisten

- Windows 10 of nieuwer
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) om de app vanuit de broncode te bouwen of uit te voeren
- [FFmpeg](https://ffmpeg.org/download.html) voor het samenvoegen van afzonderlijke YouTube-video- en audiostreams
- De officiële [yt-dlp Windows executable](https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe) voor YouTube-metadata en stream-URL's
- [Deno 2.3 of nieuwer](https://github.com/denoland/deno/releases) voor YouTubes JavaScript-challenges
- Microsoft Edge WebView2 Runtime voor de optionele YouTube-aanmelding

## Gebruik

1. Start de app.
2. Selecteer een tekstbestand met URL's, sleep dat bestand naar het venster, of selecteer de wachtrij en plak daar een gekopieerde URL-lijst met `Ctrl+V`.
3. Kies de doelmap voor de downloads.
4. Vul indien nodig een Referer-URL in.
5. Kies hoeveel downloads tegelijk mogen lopen (`1`–`20`).
6. Klik op **Start**.
7. Kies bij bestaande bestanden voor **Overschrijven**, **Overslaan** of **Hernoemen**.

Tijdens het downloaden toont de app de voortgang per bestand en van de volledige batch. Klik op **Stop** om de batch te beëindigen. Mislukte downloads worden na afloop met bestandsnaam, foutmelding en URL opgeslagen in `failed.log`.

### Tekstbestand

Zet één volledige HTTP- of HTTPS-URL per regel. Lege regels en regels die met `#` beginnen worden genegeerd.

```text
# Afbeeldingen
https://example.com/media/foto-01.jpg
https://example.com/media/foto-02.png

# Video
https://example.com/video/clip.mp4
```

Na het kiezen van een bestand verschijnen de URL's direct in de wachtrij. Je kunt dezelfde indeling ook rechtstreeks in de wachtrij plakken met `Ctrl+V` of via **URL's plakken** in het rechtermuisknopmenu. Alleen volledige `http://`- en `https://`-URL's worden verwerkt; ongeldige regels worden genegeerd. URL's uit bestanden en het klembord worden samengevoegd.

Via **Instellingen** kan de doelmap per toevoegactie worden onthouden. Een URL-bestand gebruikt dan de doelmap die eronder in het hoofdscherm staat. Geplakte tekst met volledige paden achter `#` wordt op dezelfde manier verwerkt en opent geen mapkeuze. Bevat de geplakte tekst geen padregels, dan verschijnt een mapkeuze met de doelmap uit het hoofdscherm als standaard. De kolom **Doelmap** toont vervolgens waar ieder wachtrij-item wordt opgeslagen. Dezelfde URL kan zo ook voor verschillende doelmappen worden toegevoegd. In de instellingen kan bovendien worden gekozen om voltooide regels automatisch te verwijderen wanneer nieuwe URL's worden toegevoegd.

Wanneer **Doelmap per toevoegactie onthouden** aanstaat, kan één URL-bestand meerdere doelmappen bevatten. Zet een volledig pad achter `#`; alle URL's eronder gebruiken die map totdat een volgende padregel wordt gevonden:

```text
# D:\Pictures\Camera
https://example.com/camera-1.jpg
https://example.com/camera-2.jpg
# C:\Temp
https://example.com/preview.jpg
```

URL's vóór de eerste padregel gebruiken de doelmap uit het hoofdscherm. Andere regels die met `#` beginnen blijven opmerkingen. Als de instelling uitstaat, worden alle `#`-regels als opmerkingen behandeld.

De bestandsnaam wordt afgeleid uit het pad van de URL. Als de URL geen bestandsnaam bevat, maakt de app een unieke naam in de vorm `download_<id>`.

### YouTube

Zet een normale YouTube-, korte youtu.be- of embed-URL op dezelfde manier in de wachtrij als iedere andere URL, bijvoorbeeld:

```text
https://www.youtube.com/embed/iqv7_3s83-U
```

Bij **Start** laat MediaFetch yt-dlp met Deno de videotitel en ondertekende stream-URL's ophalen. MediaFetch downloadt daarna zelf de beste video- en audiostream. FFmpeg voegt deze streams zonder hercodering samen in een MKV-container. De wachtrij toont achtereenvolgens de voortgang van de thumbnail, video, audio en het samenvoegen. Maximaal één samenvoegtaak draait tegelijk; andere downloads mogen ondertussen doorgaan.

Alle bestanden worden rechtstreeks in de gekozen doelmap geplaatst, naast afbeeldingen en andere downloads:

```text
Videotitel.video.webm
Videotitel.audio.webm
Videotitel.mkv
Videotitel.jpg
```

De werkelijke bronextensies hangen af van de door YouTube aangeboden streams. De bronbestanden blijven na succesvol samenvoegen behouden. Bij een mislukte samenvoeging blijven ze eveneens staan, zodat ze niet opnieuw hoeven te worden gedownload.

Plaats `yt-dlp.exe`, `deno.exe` en `ffmpeg.exe` naast `XiPHiAS.MediaFetch.exe`, in een submap `tools`, of voeg ze toe aan `PATH`. Voor FFmpeg wordt ook de bestaande submap `ffmpeg` ondersteund. Gebruik de officiële `yt-dlp.exe`; daarin zijn de benodigde EJS-challengescripts al opgenomen.

#### Optioneel aanmelden bij YouTube

Sommige video's vereisen alsnog een aangemelde browsersessie. Open in dat geval **Instellingen > YouTube-account > Aanmelden** en meld je rechtstreeks in het WebView2-venster bij Google/YouTube aan. MediaFetch probeert yt-dlp altijd eerst zonder cookies en maakt alleen voor een mislukte fallback tijdelijk een lokaal cookiesbestand. Dit tijdelijke bestand wordt direct na de extractoropdracht verwijderd.

De WebView2-sessie wordt opgeslagen in een afzonderlijk MediaFetch-profiel onder `%LOCALAPPDATA%\XiPHiAS\MediaFetch\WebView2\YouTube`. Wachtwoorden worden niet door MediaFetch gelezen of opgeslagen. Cookies worden niet naar `settings.json` of logbestanden geschreven. Met **Afmelden** worden de cookies uit dit profiel verwijderd.

Download alleen video's waarvoor je toestemming of downloadrechten hebt en houd rekening met de toepasselijke voorwaarden.

## Instellingen

Via **Instellingen** zijn de volgende opties beschikbaar:

- **Voltooide bestanden uit de lijst verwijderen bij het toevoegen van URL's** verwijdert regels met de status `Completed` wanneer een URL-bestand wordt toegevoegd of URL's worden geplakt. Alleen de wachtrijregels worden verwijderd; gedownloade bestanden blijven op schijf staan.
- **Doelmap per toevoegactie onthouden** bewaart bij ieder wachtrij-item de gekozen doelmap. URL-bestanden en geplakte tekst ondersteunen dan `# <volledig pad>`-secties. Bij plakken wordt alleen om een map gevraagd wanneer zulke padregels ontbreken. Als deze optie uitstaat, gebruikt de hele wachtrij bij het starten de doelmap uit het hoofdscherm.
- **Browserpreset** kiest een User-Agent voor Chrome, Edge, Firefox, Safari, Opera, Opera GX, Brave of Internet Explorer 4.
- **User-Agent** kan na het kiezen van een preset handmatig worden aangepast.
- **YouTube-account** opent optioneel een beveiligd WebView2-aanmeldvenster, toont de sessiestatus en kan de lokale YouTube-sessie weer wissen.
- **Clear-functionaliteit** bepaalt of het bevestigingsvenster standaard Completed, Failed, Skipped of Alles selecteert. Bij Alles worden de andere opties uitgeschakeld; vóór het verwijderen kan de selectie nog eenmalig worden aangepast.

## Ingebouwde handleiding

Via **Help > Handleiding** opent een helpvenster met afzonderlijke tabbladen voor:

- snel starten;
- downloaden naar meerdere doelmappen;
- uitleg van alle instellingen;
- downloadstatussen en bestandsafhandeling.
- YouTube-downloads, bronbestanden, thumbnails en FFmpeg-samenvoeging.

**Help > Over XiPHiAS MediaFetch** toont versie- en projectinformatie.

De instellingen worden lokaal opgeslagen in:

```text
%LOCALAPPDATA%\XiPHiAS\MediaFetch\settings.json
```

## Uitvoeren vanuit de broncode

```powershell
dotnet run
```

## Bouwen

```powershell
dotnet build
```

## Publiceren als zelfstandige Windows-app

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

De gepubliceerde bestanden staan vervolgens onder `bin\Release\net8.0-windows\win-x64\publish`.

## Licentie

Dit project is beschikbaar onder de [GNU General Public License v3.0](LICENSE).
