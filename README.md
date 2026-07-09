# BaroPath

**BaroPath** is a local path brain for files, folders, apps, scripts and commands.

It helps you keep important local paths organized, searchable and recoverable — without digging through millions of files by hand.

> Current interface language: **Russian only**.  
> English localization is planned for a future version.

## Features

- Save and manage paths to files, folders, apps, scripts and commands
- Organize items into collections/lists
- Drag & drop items into collections
- Favorites
- Search and quick filters
- Path health check for missing/moved files
- Everything/ES search integration
- Recovery for moved files using Everything search results
- JSON backup export/import
- Local SQLite database
- Dark UI

## Download

Go to the **Releases** page and download the latest portable build:

```text
BaroPath-v0.1.0-win-x64-portable.zip
```
Extract the archive and run:

```text
BaroPath.exe
```

## Requirements
- Windows 10/11 x64
- Everything/ES for advanced search and recovery features

BaroPath can work as a simple local path manager without Everything, but Everything integration is required for fast global file search and recovery of moved files.

## Everything / ES
BaroPath can integrate with:

- Everything.exe
- es.exe

These tools are developed by voidtools.
If they are bundled with a release build, their original license must be included in:
```text
tools/everything/License.txt
```
Do not use the Lite version of Everything for BaroPath integration, because the Lite version does not support the command line interface / IPC required by `es.exe`.

## Data storage
BaroPath stores its local data in a SQLite database.
The database and settings are stored locally on your machine. No cloud sync, no accounts, no external server.

## Backup
BaroPath supports JSON export/import for backups and migration.

## Status
This is an early public build.
Expected rough edges:

- Russian-only interface
- UI/UX may change
- No installer yet
- Portable release only
- Everything integration may require manual configuration on some systems

## License

BaroPath is licensed under the license in `LICENSE.txt`.
Third-party components are listed in `THIRD_PARTY_NOTICES.txt`.  
