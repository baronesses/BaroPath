# BaroPath

BaroPath is a Windows path manager for applications, files, folders, scripts, and commands. It keeps frequently used locations in one searchable library and can recover moved files through the embedded Everything search engine.

[Русский](#русский) · [English](#english)

## Русский

### Возможности

- Списки для группировки приложений, файлов, папок, скриптов и команд.
- Табличный и плиточный режимы отображения.
- Свои иконки или автоматическое извлечение значка из приложения.
- Поиск, фильтры, избранное, теги, заметки и проверка битых путей.
- Двойной клик для открытия и контекстное меню по правой кнопке.
- Поиск переехавших файлов через встроенные Everything и ES.
- Русский и английский интерфейс с переключением в настройках.
- Импорт и экспорт резервных копий в JSON.
- Локальная база SQLite, тёмный интерфейс, без аккаунтов и облачной синхронизации.

### Установка и обновление

1. Откройте страницу [Releases](https://github.com/baronesses/BaroPath/releases).
2. Скачайте `BaroPath-vX.Y.Z-win-x64.zip`.
3. Закройте запущенный BaroPath.
4. Распакуйте архив и замените файлы предыдущей версии.
5. Запустите `BaroPath.exe`.

База, настройки и пользовательские данные находятся в `%APPDATA%\BaroPath`, поэтому замена файлов программы их не удаляет.

### Everything внутри BaroPath

BaroPath поставляется с официальными `Everything.exe` и `es.exe` от [voidtools](https://www.voidtools.com/). При первом поиске запускается отдельный скрытый экземпляр Everything без окна и значка в системном трее. Пользователю не нужно запускать Everything вручную.

Для быстрого NTFS-индекса Everything использует собственную Windows-службу, если она установлена. Без службы поведение зависит от прав Windows и настроек индексирования.

### Сборка из исходников

Требования: Windows 10/11 x64, .NET 9 SDK и PowerShell 7 или Windows PowerShell 5.1.

```powershell
./scripts/Get-Everything.ps1
./scripts/Publish-Release.ps1
```

Готовые файлы появятся в `release/BaroPath`, а ZIP — в `release`.

## English

### Features

- Lists for applications, files, folders, scripts, and commands.
- Table and icon-grid views.
- Custom icons with automatic application-icon extraction.
- Search, filters, favorites, tags, notes, and broken-path checks.
- Double-click to open and right-click for additional actions.
- Moved-file recovery through the embedded Everything search engine.
- Live Russian and English interface switching.
- JSON backup import and export.
- Local SQLite storage, dark UI, no account, and no cloud sync.

### Install and update

1. Open the [Releases](https://github.com/baronesses/BaroPath/releases) page.
2. Download `BaroPath-vX.Y.Z-win-x64.zip`.
3. Close BaroPath if it is running.
4. Extract the archive and replace the previous application files.
5. Run `BaroPath.exe`.

The database, settings, and user data live in `%APPDATA%\BaroPath`, so replacing application files does not remove them.

### Embedded Everything

BaroPath bundles the official `Everything.exe` and `es.exe` tools from [voidtools](https://www.voidtools.com/). The first search starts a dedicated hidden Everything instance without a window or system-tray icon. Nothing needs to be launched manually.

Everything uses its Windows service for fast NTFS indexing when the service is installed. Without it, behavior depends on Windows permissions and indexing settings.

### Build from source

Requirements: Windows 10/11 x64, .NET 9 SDK, and PowerShell 7 or Windows PowerShell 5.1.

```powershell
./scripts/Get-Everything.ps1
./scripts/Publish-Release.ps1
```

The clean release is written to `release/BaroPath`, with a ZIP archive in `release`.

## Status

BaroPath is an early portable Windows build. There is no installer yet, and the UI may continue to evolve.

## License

BaroPath is distributed under [LICENSE.txt](BaroManager/LICENSE.txt). Third-party notices are listed in [THIRD_PARTY_NOTICES.txt](BaroManager/THIRD_PARTY_NOTICES.txt). Everything and ES are distributed under the bundled voidtools license.
