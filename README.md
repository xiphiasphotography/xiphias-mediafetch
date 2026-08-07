# XiPHiAS MediaFetch

A lightweight Windows bulk media downloader written in C# / .NET WinForms.

## Features

- Download URLs from a text file, one URL per line
- Download multiple files concurrently
- Resume partial downloads when the server supports HTTP range requests
- Follow redirects
- Retry failed downloads
- Show progress, file size, download speed, and estimated time remaining
- Compare existing files with the remote Content-Length when available
- Overwrite, skip, or automatically rename existing files
- Send an optional Referer header
- Use configurable browser User-Agent presets
- Avoid WebP responses unless the requested URL explicitly targets a WebP file
- Drag-and-drop a URL text file onto the window
- Write failed URLs to `failed.txt`
- Remember the last source and destination directories

## Requirements

- .NET 8 SDK
- Windows

## Run

```powershell
dotnet run
```

## Build

```powershell
dotnet build
```

## Publish

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
