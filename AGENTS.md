# AGENTS.md

## Project overview

XiPHiAS MediaFetch is a lightweight Windows bulk media downloader built with C# and .NET 8 WinForms.

The application accepts HTTP/HTTPS media URLs from text files, drag-and-drop, or clipboard input and downloads them concurrently while tracking progress, handling redirects, retries, resume support, existing files, and per-item destination folders.

Repository: `xiphiasphotography/xiphias-mediafetch`

## Technology

- C#
- .NET 8
- WinForms
- Target framework: `net8.0-windows`
- Root namespace: `XiPHiAS.MediaFetch`
- Nullable reference types are enabled.
- Implicit usings are enabled.
- The project currently has no external NuGet package dependencies.

This is a Windows desktop application. Do not introduce cross-platform abstractions unless they solve a concrete requirement.

## Build and run

From the repository root:

```powershell
dotnet restore
dotnet build
dotnet run
```

Create a self-contained x64 Windows build with:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Published output is expected under:

```text
bin\Release\net8.0-windows\win-x64\publish
```

Before considering a code change complete, run at minimum:

```powershell
dotnet build
```

There is currently no automated test project in the repository. For changes that introduce logic that can be tested independently of WinForms, prefer extracting that logic into small classes that can be covered by tests later rather than adding more logic directly to the form.

## Repository structure

Important files:

- `Program.cs` — application entry point.
- `MainForm.cs` — main window, queue management, user interaction, batch orchestration, progress display, and most application workflow.
- `Downloader.cs` — HTTP download implementation, redirects, decompression, retries, resume support, content-length probing, and request headers.
- `DownloadItem.cs` — mutable queue/download state for a single item.
- `AppSettings.cs` — JSON-backed user settings stored under `%LOCALAPPDATA%\XiPHiAS\MediaFetch\settings.json`.
- `SettingsDialog.cs` — settings UI.
- `ExistingFilesDialog.cs` — existing-file handling UI.
- `ClearQueueDialog.cs` — queue clearing options.
- `HelpDialog.cs` — built-in user documentation.
- `AboutDialog.cs` — application/version information.
- `Branding.cs` — embedded branding asset loading.
- `Assets/` — application icon and logo.
- `README.md` — public project documentation.
- `XiPHiAS.MediaFetch.csproj` — framework, assembly metadata, version, icon, and embedded resources.

## UI conventions

The WinForms UI is created programmatically in C#.

- Do not add `.Designer.cs`, `.resx`, or Visual Studio Designer-generated form code unless explicitly requested.
- Follow the existing layout approach with `TableLayoutPanel`, standard WinForms controls, DPI scaling, and system colors.
- Keep user-facing application text in Dutch unless a feature explicitly requires another language.
- Preserve keyboard, clipboard, drag-and-drop, tooltip, and context-menu workflows when changing queue behavior.
- Avoid blocking the UI thread. Network, file, and potentially slow resolver work must be asynchronous.
- UI updates originating from background work must be marshalled safely to the UI thread using the existing WinForms patterns.

`MainForm.cs` is already large. When adding a substantial new feature, prefer a dedicated class/service instead of expanding the form with protocol-specific implementation details.

## Download architecture

Keep responsibilities separated:

- `MainForm` should orchestrate UI and queue state.
- `Downloader` should remain responsible for downloading an already-resolved direct URL.
- Source-specific URL discovery or parsing should live in separate resolver classes and produce normal `DownloadItem` inputs for the existing downloader whenever practical.

Do not make `Downloader` aware of Facebook albums, HTML pages, or other source-specific discovery unless the behavior is truly generic to every download.

### Existing behavior that must be preserved

Unless the task explicitly changes it, retain:

- HTTP and HTTPS URL support.
- Automatic redirects.
- gzip, deflate, and Brotli decompression.
- 1–20 concurrent downloads, default 4.
- Up to 3 attempts for failed downloads.
- The current corrected-URL retry behavior.
- HTTP Range resume support.
- Correct fallback to a fresh download when a server ignores a Range request and returns HTTP 200.
- Existing-file detection using remote `Content-Length` when available.
- Overwrite, skip, and rename workflows.
- Optional Referer header.
- Configurable User-Agent.
- Avoid requesting WebP unless the requested URL explicitly targets WebP.
- `failed.txt` output for failed URLs.
- Cancellation/Stop behavior.
- Per-file and total progress reporting.

When modifying HTTP behavior, account for servers that do not support `HEAD`, `Range`, or `Content-Length`. Lack of one of these capabilities must not by itself make a valid download fail.

## Queue rules

Queue uniqueness is based on the source URL plus the remembered destination context.

When adding new URL sources:

- Normalize/resolve them before adding final media downloads where possible.
- Do not silently create duplicate queue rows for the same resolved item and destination.
- Preserve the user's selected destination folder.
- Sanitize generated filenames using Windows filename rules.
- Do not overwrite user files without passing through the application's existing-file policy.
- Keep queue state and visible `ListView` state synchronized.

Do not delete downloaded files when clearing queue entries.

## Settings

`AppSettings` is intentionally tolerant of missing, malformed, inaccessible, or older settings files.

When adding settings:

- Give new properties safe defaults so existing `settings.json` files continue to load.
- Do not make a newly introduced setting mandatory.
- Preserve the current behavior where settings failures do not prevent the application from running.
- Store ordinary application preferences in the existing settings file rather than creating additional config files without a clear reason.
- Never store passwords, authentication tokens, or session secrets in plain-text settings.

## Source-specific resolvers

MediaFetch may gain resolvers for URLs that are not themselves direct media files, for example Facebook album URLs.

Implement these as a discovery stage before the existing download stage.

Preferred shape:

```text
User input
    -> source detection
    -> source-specific resolver
    -> direct media URLs + metadata
    -> DownloadItem queue
    -> existing Downloader
```

A resolver should:

- be cancellable;
- perform network/browser work asynchronously;
- expose useful progress/status information;
- return stable metadata where available, such as source ID, filename, dimensions, and resolved direct URL;
- select the highest-quality source-supported media variant rather than guessing URL transformations;
- deduplicate results;
- fail individual items cleanly where possible instead of aborting an entire album/batch.

For Facebook support specifically, do not depend on obsolete filename tricks such as replacing legacy size suffixes. Prefer URLs/resources that Facebook itself exposes to the authenticated browser session.

If browser automation is introduced, keep browser/session handling isolated from `Downloader` and avoid storing Facebook credentials in the application.

## Networking and cancellation

- Reuse `HttpClient` for a downloader lifetime; do not create one client per chunk or request.
- Use `HttpCompletionOption.ResponseHeadersRead` for large media downloads.
- Pass `CancellationToken` through async operations.
- Treat user cancellation separately from failures.
- Dispose responses, requests, streams, and linked cancellation sources deterministically.
- Avoid arbitrary short global request timeouts for downloads. Targeted probe operations may use their own bounded timeout.
- Do not buffer complete media files in memory.

## Error handling

User-facing failures should be actionable and concise.

- Do not swallow download errors that the user needs to know about.
- It is acceptable to ignore failures for optional conveniences such as saving window/settings state, as the current code does.
- Avoid broad `catch (Exception)` unless the operation genuinely needs a final boundary; prefer expected exception types where practical.
- Never turn a cancellation into a generic failure message.
- Partial downloads should remain resumable when safe.

## Coding style

Follow the existing repository style.

- File-scoped namespace: `namespace XiPHiAS.MediaFetch;`
- Four-space indentation.
- Braces on their own lines.
- Prefer clear, descriptive names over abbreviations.
- Use `var` where the type is evident from the right-hand side.
- Use collection expressions where they improve readability.
- Use nullable annotations correctly; do not silence nullable warnings without understanding them.
- Keep methods focused. Extract helpers/services when a workflow becomes difficult to follow.
- Add comments for non-obvious protocol or compatibility behavior, not for self-explanatory code.
- Preserve Dutch comments where they explain application-specific behavior.

Do not perform unrelated refactors in the same change.

## Documentation

Update documentation when behavior visible to users changes.

Depending on the change, review:

- `README.md`
- `HelpDialog.cs`
- `SettingsDialog.cs`
- `AboutDialog.cs`

New workflows should be documented in both the README and built-in Help when appropriate.

Keep examples accurate for the current UI and feature set.

## Versioning

The application version is defined in `XiPHiAS.MediaFetch.csproj` using:

- `Version`
- `AssemblyVersion`
- `FileVersion`
- `InformationalVersion`

Do not bump the version merely because code was edited. Change it only when requested or when preparing an explicit release/version update, and keep all version fields consistent.

## Assets and branding

Preserve the XiPHiAS MediaFetch name and existing branding unless explicitly asked to change them.

- `Assets/App.ico` is the application icon.
- `Assets/XiPHiAS.MediaFetch-logo.png` is embedded as `XiPHiAS.MediaFetch.Logo.png`.

Do not replace, regenerate, optimize, or re-encode branding assets as part of unrelated work.

## Git hygiene

Ignored/generated directories include:

- `.source/`
- `bin/`
- `obj/`
- `publish/`
- `Release/`
- `Samples/`
- `.vs/`
- `.vscode/`

Do not commit generated build or publish output.

Keep commits scoped to the requested change and avoid formatting unrelated files.

## Completion checklist

For a normal code change:

1. Review the affected existing workflow before editing.
2. Keep UI, resolver, and download responsibilities separated.
3. Preserve cancellation, resume, retry, and existing-file behavior unless intentionally changing them.
4. Update README/built-in Help for user-visible behavior.
5. Run `dotnet build`.
6. Resolve build warnings/errors introduced by the change.
7. Do not commit generated artifacts.
