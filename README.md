# XiPHiAS MediaFetch

XiPHiAS MediaFetch is een lichte Windows-app voor het gelijktijdig downloaden van afbeeldingen, video's en andere mediabestanden uit een URL-lijst. De app is gebouwd met C# en .NET 8 WinForms.

## Functies

- Lees URL's uit een tekstbestand (`.txt`), één URL per regel.
- Plak een gekopieerde lijst met URL's met `Ctrl+V` rechtstreeks in de wachtrij.
- Negeer lege regels, commentaarregels die met `#` beginnen en dubbele URL's.
- Download 1 tot 20 bestanden tegelijk (standaard: 4).
- Hervat gedeeltelijke downloads als de server HTTP-rangeverzoeken ondersteunt.
- Volg redirects en pak gzip-, deflate- en Brotli-responses uit.
- Probeer een mislukte download maximaal drie keer, met oplopende wachttijd.
- Corrigeer na een mislukte eerste poging HTML-entiteiten en onjuiste URL-padencoding automatisch (bijvoorbeeld `&amp;` naar `%26`).
- Toon per bestand de voortgang, grootte, snelheid, resterende tijd en status.
- Schakel **Start** uit wanneer de wachtrij leeg is of alle items zijn voltooid of overgeslagen.
- Stop een actieve downloadbatch vanuit de interface.
- Controleer bestaande bestanden aan de hand van de externe `Content-Length`, indien beschikbaar.
- Overschrijf, sla over of hernoem bestaande doelbestanden automatisch.
- Stel optioneel een Referer-URL in voor servers die directe downloads weigeren.
- Kies een browser-User-Agent-preset of vul een eigen User-Agent in.
- Vermijd WebP-responses, tenzij de gevraagde URL expliciet op `.webp` eindigt.
- Sleep een URL-bestand naar het hoofdvenster.
- Schrijf URL's van mislukte downloads naar `failed.txt` in de doelmap.
- Onthoud de laatst gebruikte bron- en doelmap.
- Onthoud de vensterpositie, venstergrootte en maximalisatiestatus.
- Controleer een ingevoerde doelmap direct en bied aan een ontbrekende map aan te maken.

## Vereisten

- Windows 10 of nieuwer
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) om de app vanuit de broncode te bouwen of uit te voeren

## Gebruik

1. Start de app.
2. Selecteer een tekstbestand met URL's, sleep dat bestand naar het venster, of selecteer de wachtrij en plak daar een gekopieerde URL-lijst met `Ctrl+V`.
3. Kies de doelmap voor de downloads.
4. Vul indien nodig een Referer-URL in.
5. Kies hoeveel downloads tegelijk mogen lopen (`1`–`20`).
6. Klik op **Start**.
7. Kies bij bestaande bestanden voor **Overschrijven**, **Overslaan** of **Hernoemen**.

Tijdens het downloaden toont de app de voortgang per bestand en van de volledige batch. Klik op **Stop** om de batch te beëindigen. Eventuele mislukte URL's worden na afloop opgeslagen in `failed.txt`.

### Tekstbestand

Zet één volledige HTTP- of HTTPS-URL per regel. Lege regels en regels die met `#` beginnen worden genegeerd.

```text
# Afbeeldingen
https://example.com/media/foto-01.jpg
https://example.com/media/foto-02.png

# Video
https://example.com/video/clip.mp4
```

Na het kiezen van een bestand verschijnen de URL's direct in de wachtrij. Je kunt dezelfde indeling ook rechtstreeks in de wachtrij plakken met `Ctrl+V` of via **URL's plakken** in het rechtermuisknopmenu. Alleen volledige `http://`- en `https://`-URL's worden verwerkt; ongeldige regels worden genegeerd. URL's uit bestanden en het klembord worden samengevoegd en dubbele URL's verschijnen maar één keer.

De bestandsnaam wordt afgeleid uit het pad van de URL. Als de URL geen bestandsnaam bevat, maakt de app een unieke naam in de vorm `download_<id>`.

## Instellingen

Via **Instellingen** kun je een browserpreset kiezen voor Chrome, Edge, Firefox, Safari, Opera, Opera GX, Brave of Internet Explorer 4. De bijbehorende User-Agent kan daarna handmatig worden aangepast.

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
