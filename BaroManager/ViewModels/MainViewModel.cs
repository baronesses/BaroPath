using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using BaroManager.Data;
using BaroManager.Models;
using BaroManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfClipboard = System.Windows.Clipboard;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;
using WpfMessageBoxResult = System.Windows.MessageBoxResult;
using WinForms = System.Windows.Forms;

namespace BaroManager.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly AppDbContext _db;

    public ObservableCollection<ManagedItem> Items { get; } = new();
    
    public bool HasSelectedItem => SelectedItem is not null;

    public bool HasSelectedEverythingResult => SelectedEverythingResult is not null;

    public bool CanUseEverythingPath => SelectedItem is not null && SelectedEverythingResult is not null;

    public ObservableCollection<ManagedCollection> Collections { get; } = new();
    
    public ObservableCollection<EverythingSearchResult> EverythingResults { get; } = new();

    public ObservableCollection<LocalizedOption> ItemTypes { get; } = new();

    public ObservableCollection<LocalizedOption> FilterModes { get; } = new();

    public string[] LanguageOptions { get; } = ["Русский", "English"];

    public string MainActionText => EditingItemId is null
        ? LocalizationService.Get("Action.Add")
        : LocalizationService.Get("Action.Save");

    public string FormTitleText => EditingItemId is null
        ? LocalizationService.Get("Form.NewItem")
        : LocalizationService.Get("Form.EditItem");

    public string SelectedCollectionDisplayName =>
        SelectedCollection?.DisplayName ?? LocalizationService.Get("Lists.AllItems");

    [ObservableProperty]
    private string searchText = string.Empty;
    
    [ObservableProperty]
    private string everythingQuery = string.Empty;

    [ObservableProperty]
    private int everythingMaxResults = 100;

    [ObservableProperty]
    private EverythingSearchResult? selectedEverythingResult;
    
    [ObservableProperty]
    private bool isItemFormExpanded;

    [ObservableProperty]
    private bool showOnlyMissing;
    
    [ObservableProperty]
    private string selectedFilterMode = "All";

    [ObservableProperty]
    private string newTitle = string.Empty;

    [ObservableProperty]
    private string newPath = string.Empty;

    [ObservableProperty]
    private string selectedItemType = "Folder";

    [ObservableProperty]
    private string newArguments = string.Empty;

    [ObservableProperty]
    private string newWorkingDirectory = string.Empty;

    [ObservableProperty]
    private string newTags = string.Empty;

    [ObservableProperty]
    private string newNote = string.Empty;

    [ObservableProperty]
    private string newIconPath = string.Empty;
    
    [ObservableProperty]
    private string settingsEsPath = string.Empty;

    [ObservableProperty]
    private string settingsEverythingPath = string.Empty;

    [ObservableProperty]
    private bool settingsAutoStartEverything = true;

    [ObservableProperty]
    private string selectedLanguage = "Русский";

    [ObservableProperty]
    private bool isGridView;

    public bool IsListView => !IsGridView;

    public string SettingsFilePath => AppSettingsService.SettingsPath;

    [ObservableProperty]
    private bool newIsFavorite;

    [ObservableProperty]
    private bool newRunOnAppStart;

    [ObservableProperty]
    private ManagedItem? selectedItem;

    [ObservableProperty]
    private string newCollectionName = string.Empty;

    [ObservableProperty]
    private string selectedCollectionName = string.Empty;

    [ObservableProperty]
    private ManagedCollection? selectedCollection;

    [ObservableProperty]
    private ManagedCollection? selectedTargetCollection;

    [ObservableProperty]
    private int? editingItemId;

    public MainViewModel(AppDbContext db)
    {
        _db = db;
        
        LoadSettingsToFields();
        ReloadLocalizedOptions();

        LoadCollections();
        LoadItems();
    }

    partial void OnSearchTextChanged(string value)
    {
        LoadItems();
    }
    
    partial void OnSelectedItemChanged(ManagedItem? value)
    {
        OnPropertyChanged(nameof(HasSelectedItem));
        OnPropertyChanged(nameof(CanUseEverythingPath));
    }

    partial void OnShowOnlyMissingChanged(bool value)
    {
        LoadItems();
    }
    
    partial void OnSelectedEverythingResultChanged(EverythingSearchResult? value)
    {
        OnPropertyChanged(nameof(HasSelectedEverythingResult));
        OnPropertyChanged(nameof(CanUseEverythingPath));
    }
    
    partial void OnSelectedFilterModeChanged(string value)
    {
        LoadItems();
    }

    partial void OnSelectedCollectionChanged(ManagedCollection? value)
    {
        SelectedCollectionName = value?.Name ?? string.Empty;
        OnPropertyChanged(nameof(SelectedCollectionDisplayName));
        LoadItems();
    }

    partial void OnEditingItemIdChanged(int? value)
    {
        OnPropertyChanged(nameof(MainActionText));
        OnPropertyChanged(nameof(FormTitleText));
    }

    partial void OnIsGridViewChanged(bool value)
    {
        OnPropertyChanged(nameof(IsListView));
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        var language = value == "English" ? "en" : "ru";
        LocalizationService.ApplyLanguage(language);
        ReloadLocalizedOptions();

        OnPropertyChanged(nameof(MainActionText));
        OnPropertyChanged(nameof(FormTitleText));
        OnPropertyChanged(nameof(SelectedCollectionDisplayName));

        var settings = AppSettingsService.Load();
        settings.Language = language;
        AppSettingsService.Save(settings);

        LoadItems();
        RefreshEverythingResults();
    }

    private void ReloadLocalizedOptions()
    {
        var currentItemType = string.IsNullOrWhiteSpace(SelectedItemType)
            ? "Folder"
            : SelectedItemType;

        var currentFilterMode = string.IsNullOrWhiteSpace(SelectedFilterMode)
            ? "All"
            : SelectedFilterMode;

        ReplaceOptions(ItemTypes,
        [
            new("Folder", LocalizationService.Get("Type.Folder")),
            new("File", LocalizationService.Get("Type.File")),
            new("App", LocalizationService.Get("Type.App")),
            new("Script", LocalizationService.Get("Type.Script")),
            new("Command", LocalizationService.Get("Type.Command"))
        ]);

        ReplaceOptions(FilterModes,
        [
            new("All", LocalizationService.Get("Filter.All")),
            new("Favorites", LocalizationService.Get("Filter.Favorites")),
            new("Missing", LocalizationService.Get("Filter.Missing")),
            new("Commands", LocalizationService.Get("Filter.Commands")),
            new("Startup", LocalizationService.Get("Filter.Startup")),
            new("Folders", LocalizationService.Get("Filter.Folders")),
            new("Scripts", LocalizationService.Get("Filter.Scripts")),
            new("Apps", LocalizationService.Get("Filter.Apps"))
        ]);

        SelectedItemType = currentItemType;
        SelectedFilterMode = currentFilterMode;
    }

    private static void ReplaceOptions(
        ObservableCollection<LocalizedOption> target,
        IEnumerable<LocalizedOption> options)
    {
        target.Clear();

        foreach (var option in options)
            target.Add(option);
    }

    private void RefreshEverythingResults()
    {
        var results = EverythingResults.ToList();
        EverythingResults.Clear();

        foreach (var result in results)
            EverythingResults.Add(result);
    }

    [RelayCommand]
    private void LoadCollections()
    {
        var selectedCollectionId = SelectedCollection?.Id;
        var selectedTargetCollectionId = SelectedTargetCollection?.Id;

        Collections.Clear();

        var itemCounts = _db.ItemCollections
            .AsNoTracking()
            .GroupBy(x => x.CollectionId)
            .Select(x => new
            {
                CollectionId = x.Key,
                Count = x.Count()
            })
            .ToDictionary(x => x.CollectionId, x => x.Count);

        var collections = _db.ManagedCollections
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList();

        foreach (var collection in collections)
        {
            collection.ItemCount = itemCounts.TryGetValue(collection.Id, out var count)
                ? count
                : 0;

            Collections.Add(collection);
        }

        if (selectedCollectionId is not null)
            SelectedCollection = Collections.FirstOrDefault(x => x.Id == selectedCollectionId.Value);

        if (selectedTargetCollectionId is not null)
            SelectedTargetCollection = Collections.FirstOrDefault(x => x.Id == selectedTargetCollectionId.Value);

        SelectedCollectionName = SelectedCollection?.Name ?? string.Empty;
    }

    [RelayCommand]
    private void AddCollection()
    {
        if (string.IsNullOrWhiteSpace(NewCollectionName))
        {
            WpfMessageBox.Show(LocalizationService.Get("Message.ListNameEmpty"), "BaroPath");
            return;
        }

        var name = NewCollectionName.Trim();

        if (_db.ManagedCollections.Any(x => x.Name == name))
        {
            WpfMessageBox.Show(LocalizationService.Get("Message.ListExists"), "BaroPath");
            return;
        }

        var collection = new ManagedCollection
        {
            Name = name,
            CreatedAt = DateTime.Now
        };

        _db.ManagedCollections.Add(collection);
        _db.SaveChanges();

        NewCollectionName = string.Empty;

        LoadCollections();

        var createdCollection = Collections.FirstOrDefault(x => x.Id == collection.Id);

        SelectedCollection = createdCollection;
        SelectedTargetCollection = createdCollection;
    }
    
    
    
    private void LoadSettingsToFields()
{
    var settings = AppSettingsService.Load();

    SettingsEsPath = settings.EverythingEsPath;
    SettingsEverythingPath = settings.EverythingExePath;
    SettingsAutoStartEverything = settings.AutoStartEverything;
    IsGridView = string.Equals(settings.ItemViewMode, "Grid", StringComparison.OrdinalIgnoreCase);
    SelectedLanguage = LocalizationService.NormalizeLanguage(settings.Language) == "en"
        ? "English"
        : "Русский";
}

[RelayCommand]
private void SaveSettings()
{
    var settings = new AppSettings
    {
        EverythingEsPath = SettingsEsPath?.Trim() ?? string.Empty,
        EverythingExePath = SettingsEverythingPath?.Trim() ?? string.Empty,
        AutoStartEverything = SettingsAutoStartEverything,
        Language = SelectedLanguage == "English" ? "en" : "ru",
        Theme = "dark",
        ItemViewMode = IsGridView ? "Grid" : "List"
    };

    AppSettingsService.Save(settings);

    WpfMessageBox.Show(
        LocalizationService.Get("Message.SettingsSaved"),
        LocalizationService.Get("Title.Settings")
    );
}

[RelayCommand]
private void ShowListView()
{
    if (!IsGridView)
        return;

    IsGridView = false;
    PersistItemViewMode();
}

[RelayCommand]
private void ShowGridView()
{
    if (IsGridView)
        return;

    IsGridView = true;
    PersistItemViewMode();
}

private void PersistItemViewMode()
{
    var settings = AppSettingsService.Load();
    settings.ItemViewMode = IsGridView ? "Grid" : "List";
    AppSettingsService.Save(settings);
}

[RelayCommand]
private void ChooseEsPath()
{
    var dialog = new OpenFileDialog
    {
        Title = LocalizationService.Get("Message.ChooseEs"),
        Filter = "es.exe|es.exe|Executable files (*.exe)|*.exe|All files (*.*)|*.*",
        CheckFileExists = true
    };

    if (dialog.ShowDialog() != true)
        return;

    SettingsEsPath = dialog.FileName;
}

[RelayCommand]
private void ChooseEverythingPath()
{
    var dialog = new OpenFileDialog
    {
        Title = LocalizationService.Get("Message.ChooseEverything"),
        Filter = "Everything.exe|Everything.exe|Executable files (*.exe)|*.exe|All files (*.*)|*.*",
        CheckFileExists = true
    };

    if (dialog.ShowDialog() != true)
        return;

    SettingsEverythingPath = dialog.FileName;
}

[RelayCommand]
private void TestEverythingSettings()
{
    SaveSettings();

    try
    {
        var esPath = EverythingSearchService.GetResolvedEsPath();
        var everythingPath = EverythingSearchService.GetResolvedEverythingPath();

        var results = EverythingSearchService.Search("*", 1);

        WpfMessageBox.Show(
            LocalizationService.Format("Message.EverythingWorks", esPath, everythingPath, results.Count),
            "Everything test"
        );
    }
    catch (Exception ex)
    {
        WpfMessageBox.Show(
            ex.Message,
            "Everything test error"
        );
    }
}

[RelayCommand]
private void OpenSettingsFolder()
{
    try
    {
        Directory.CreateDirectory(AppSettingsService.SettingsDirectory);

        Process.Start(new ProcessStartInfo
        {
            FileName = AppSettingsService.SettingsDirectory,
            UseShellExecute = true
        });
    }
    catch (Exception ex)
    {
        WpfMessageBox.Show(
            ex.Message,
            "Settings folder error"
        );
    }
}

[RelayCommand]
private void OpenToolsFolder()
{
    try
    {
        Directory.CreateDirectory(AppSettingsService.ToolsEverythingDirectory);

        Process.Start(new ProcessStartInfo
        {
            FileName = AppSettingsService.ToolsEverythingDirectory,
            UseShellExecute = true
        });
    }
    catch (Exception ex)
    {
        WpfMessageBox.Show(
            ex.Message,
            "Tools folder error"
        );
    }
}
    
    [RelayCommand]
private void ExportBackup()
{
    var dialog = new SaveFileDialog
    {
        Title = LocalizationService.Get("Message.ChooseBackupSave"),
        Filter = "BaroManager backup (*.json)|*.json|JSON (*.json)|*.json|All files (*.*)|*.*",
        FileName = $"baromanager-backup-{DateTime.Now:yyyyMMdd-HHmmss}.json"
    };

    if (dialog.ShowDialog() != true)
        return;

    try
    {
        BackupService.Export(_db, dialog.FileName);

        WpfMessageBox.Show(
            LocalizationService.Format("Message.BackupSaved", dialog.FileName),
            LocalizationService.Get("Title.Backup")
        );
    }
    catch (Exception ex)
    {
        WpfMessageBox.Show(
            ex.Message,
            "Backup export error"
        );
    }
}

[RelayCommand]
private void ImportBackup()
{
    var dialog = new OpenFileDialog
    {
        Title = LocalizationService.Get("Message.ChooseBackupOpen"),
        Filter = "BaroManager backup (*.json)|*.json|JSON (*.json)|*.json|All files (*.*)|*.*",
        CheckFileExists = true
    };

    if (dialog.ShowDialog() != true)
        return;

    var answer = WpfMessageBox.Show(
        LocalizationService.Get("Message.ImportConfirm"),
        LocalizationService.Get("Title.ImportBackup"),
        WpfMessageBoxButton.YesNo,
        WpfMessageBoxImage.Question
    );

    if (answer != WpfMessageBoxResult.Yes)
        return;

    try
    {
        var result = BackupService.Import(_db, dialog.FileName);

        LoadCollections();
        LoadItems();

        WpfMessageBox.Show(
            LocalizationService.Format(
                "Message.ImportComplete",
                result.CollectionsCreated,
                result.ItemsCreated,
                result.ItemsUpdated,
                result.LinksCreated),
            LocalizationService.Get("Title.ImportBackup")
        );
    }
    catch (Exception ex)
    {
        WpfMessageBox.Show(
            ex.Message,
            "Backup import error"
        );
    }
}

[RelayCommand]
private void OpenDatabaseFolder()
{
    try
    {
        Directory.CreateDirectory(AppDbContext.DatabaseDirectory);

        Process.Start(new ProcessStartInfo
        {
            FileName = AppDbContext.DatabaseDirectory,
            UseShellExecute = true
        });
    }
    catch (Exception ex)
    {
        WpfMessageBox.Show(
            ex.Message,
            "Database folder error"
        );
    }
}

    [RelayCommand]
    private void RenameSelectedCollection()
    {
        if (SelectedCollection is null)
        {
            WpfMessageBox.Show(LocalizationService.Get("Message.SelectListLeft"), "BaroPath");
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedCollectionName))
        {
            WpfMessageBox.Show(LocalizationService.Get("Message.ListNameEmpty"), "BaroPath");
            return;
        }

        var newName = SelectedCollectionName.Trim();

        var duplicate = _db.ManagedCollections.Any(x =>
            x.Id != SelectedCollection.Id &&
            x.Name == newName
        );

        if (duplicate)
        {
            WpfMessageBox.Show(LocalizationService.Get("Message.ListDuplicate"), "BaroPath");
            return;
        }

        var entity = _db.ManagedCollections.FirstOrDefault(x => x.Id == SelectedCollection.Id);

        if (entity is null)
        {
            WpfMessageBox.Show(LocalizationService.Get("Message.ListNotFound"), "BaroPath");
            LoadCollections();
            return;
        }

        entity.Name = newName;
        _db.SaveChanges();

        var selectedId = entity.Id;

        LoadCollections();

        SelectedCollection = Collections.FirstOrDefault(x => x.Id == selectedId);
    }

    [RelayCommand]
    private void DeleteSelectedCollection()
    {
        if (SelectedCollection is null)
        {
            WpfMessageBox.Show(LocalizationService.Get("Message.SelectListLeft"), "BaroPath");
            return;
        }

        var result = WpfMessageBox.Show(
            LocalizationService.Format("Message.DeleteListConfirm", SelectedCollection.Name),
            LocalizationService.Get("Message.DeleteListTitle"),
            WpfMessageBoxButton.YesNo,
            WpfMessageBoxImage.Question
        );

        if (result != WpfMessageBoxResult.Yes)
            return;

        var collectionId = SelectedCollection.Id;

        var entity = _db.ManagedCollections.FirstOrDefault(x => x.Id == collectionId);

        if (entity is null)
        {
            WpfMessageBox.Show(LocalizationService.Get("Message.ListAlreadyGone"), "BaroPath");
            LoadCollections();
            LoadItems();
            return;
        }

        _db.ManagedCollections.Remove(entity);
        _db.SaveChanges();

        if (SelectedTargetCollection?.Id == collectionId)
            SelectedTargetCollection = null;

        SelectedCollection = null;
        SelectedCollectionName = string.Empty;

        LoadCollections();
        LoadItems();
    }

    [RelayCommand]
    private void ShowAll()
    {
        SelectedCollection = null;
        LoadItems();
    }

    [RelayCommand]
    private void CheckPaths()
    {
        var items = _db.ManagedItems.ToList();

        var ok = 0;
        var missing = 0;
        var workDirMissing = 0;
        var commands = 0;
        var checkedAt = DateTime.Now;

        foreach (var item in items)
        {
            var result = EvaluatePathStatus(item);

            item.ExistsNow = result.ExistsNow;
            item.PathStatus = result.PathStatus;
            item.LastCheckedAt = checkedAt;
            item.UpdatedAt = checkedAt;

            switch (result.PathStatus)
            {
                case "OK":
                    ok++;
                    break;

                case "Missing":
                    missing++;
                    break;

                case "WorkDirMissing":
                    workDirMissing++;
                    break;

                case "Command":
                    commands++;
                    break;
            }
        }

        _db.SaveChanges();

        LoadItems();

        WpfMessageBox.Show(
            LocalizationService.Format("Message.CheckComplete", ok, missing, workDirMissing, commands),
            LocalizationService.Get("Title.CheckPaths")
        );
    }

    [RelayCommand]
    private void LoadItems()
    {
        Items.Clear();

        var query = _db.ManagedItems
            .AsNoTracking()
            .AsQueryable();

        if (SelectedCollection is not null)
        {
            var collectionId = SelectedCollection.Id;

            var itemIdsInCollection = _db.ItemCollections
                .AsNoTracking()
                .Where(x => x.CollectionId == collectionId)
                .Select(x => x.ManagedItemId);

            query = query.Where(x => itemIdsInCollection.Contains(x.Id));
        }

        if (ShowOnlyMissing)
        {
            query = query.Where(x =>
                x.PathStatus == "Missing" ||
                x.PathStatus == "WorkDirMissing"
            );
        }

        query = SelectedFilterMode switch
        {
            "Favorites" => query.Where(x => x.IsFavorite),

            "Missing" => query.Where(x =>
                x.PathStatus == "Missing" ||
                x.PathStatus == "WorkDirMissing"
            ),

            "Commands" => query.Where(x => x.ItemType == "Command"),

            "Startup" => query.Where(x => x.RunOnAppStart),

            "Folders" => query.Where(x => x.ItemType == "Folder"),

            "Scripts" => query.Where(x => x.ItemType == "Script"),

            "Apps" => query.Where(x => x.ItemType == "App"),

            _ => query
        };

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();

            query = query.Where(x =>
                x.Title.Contains(search) ||
                x.Path.Contains(search) ||
                x.ItemType.Contains(search) ||
                x.PathStatus.Contains(search) ||
                (x.Tags != null && x.Tags.Contains(search)) ||
                (x.Note != null && x.Note.Contains(search))
            );
        }

        var items = query
            .OrderByDescending(x => x.IsFavorite)
            .ThenBy(x => x.PathStatus == "Missing" ? 0 : x.PathStatus == "WorkDirMissing" ? 1 : 2)
            .ThenByDescending(x => x.LastUsedAt)
            .ThenBy(x => x.Title)
            .ToList();

        foreach (var item in items)
            Items.Add(item);
    }

    [RelayCommand]
    private void AddItem()
    {
        if (EditingItemId is not null)
        {
            UpdateEditingItem();
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPath))
        {
            WpfMessageBox.Show(LocalizationService.Get("Message.PathEmpty"), "BaroPath");
            return;
        }

        var cleanPath = NewPath.Trim();

        if (SelectedItemType != "Command" &&
            !File.Exists(cleanPath) &&
            !Directory.Exists(cleanPath))
        {
            var result = WpfMessageBox.Show(
                LocalizationService.Get("Message.PathMissingConfirm"),
                LocalizationService.Get("Message.PathMissingTitle"),
                WpfMessageBoxButton.YesNo,
                WpfMessageBoxImage.Warning
            );

            if (result != WpfMessageBoxResult.Yes)
                return;
        }

        var existingItem = _db.ManagedItems.FirstOrDefault(x => x.Path == cleanPath);

        if (existingItem is not null)
        {
            if (SelectedCollection is not null)
            {
                var linked = LinkItemToCollection(existingItem.Id, SelectedCollection.Id);

                if (linked)
                {
                    WpfMessageBox.Show(
                        LocalizationService.Get("Message.ExistingAddedToList"),
                        "BaroPath"
                    );

                    ClearForm();
                    LoadCollections();
                    LoadItems();
                    return;
                }
            }

            WpfMessageBox.Show(LocalizationService.Get("Message.PathDuplicate"), "BaroPath");
            return;
        }

        var title = string.IsNullOrWhiteSpace(NewTitle)
            ? GuessTitle(cleanPath)
            : NewTitle.Trim();

        var status = EvaluatePathStatus(cleanPath, SelectedItemType, NewWorkingDirectory);

        var item = new ManagedItem
        {
            Title = title,
            Path = cleanPath,
            ItemType = SelectedItemType,
            Arguments = string.IsNullOrWhiteSpace(NewArguments) ? null : NewArguments.Trim(),
            WorkingDirectory = string.IsNullOrWhiteSpace(NewWorkingDirectory) ? null : NewWorkingDirectory.Trim(),
            Tags = string.IsNullOrWhiteSpace(NewTags) ? null : NewTags.Trim(),
            Note = string.IsNullOrWhiteSpace(NewNote) ? null : NewNote.Trim(),
            IconPath = string.IsNullOrWhiteSpace(NewIconPath) ? null : NewIconPath.Trim(),
            IsFavorite = NewIsFavorite,
            RunOnAppStart = NewRunOnAppStart,
            ExistsNow = status.ExistsNow,
            PathStatus = status.PathStatus,
            LastCheckedAt = DateTime.Now,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _db.ManagedItems.Add(item);
        _db.SaveChanges();

        if (SelectedCollection is not null)
        {
            LinkItemToCollection(item.Id, SelectedCollection.Id);
        }

        ClearForm();
        LoadCollections();
        LoadItems();
    }

    private void UpdateEditingItem()
    {
        if (EditingItemId is null)
            return;

        if (string.IsNullOrWhiteSpace(NewPath))
        {
            WpfMessageBox.Show(LocalizationService.Get("Message.PathEmpty"), "BaroPath");
            return;
        }

        var cleanPath = NewPath.Trim();

        if (SelectedItemType != "Command" &&
            !File.Exists(cleanPath) &&
            !Directory.Exists(cleanPath))
        {
            var result = WpfMessageBox.Show(
                LocalizationService.Get("Message.PathMissingConfirm"),
                LocalizationService.Get("Message.PathMissingTitle"),
                WpfMessageBoxButton.YesNo,
                WpfMessageBoxImage.Warning
            );

            if (result != WpfMessageBoxResult.Yes)
                return;
        }

        var entity = _db.ManagedItems.FirstOrDefault(x => x.Id == EditingItemId.Value);

        if (entity is null)
        {
            WpfMessageBox.Show(LocalizationService.Get("Message.ItemNotFound"), "BaroPath");
            ClearForm();
            LoadItems();
            return;
        }

        var duplicate = _db.ManagedItems.FirstOrDefault(x =>
            x.Id != entity.Id &&
            x.Path == cleanPath
        );

        if (duplicate is not null)
        {
            WpfMessageBox.Show(
                LocalizationService.Get("Message.OtherItemDuplicate"),
                "BaroPath"
            );
            return;
        }

        var status = EvaluatePathStatus(cleanPath, SelectedItemType, NewWorkingDirectory);

        entity.Title = string.IsNullOrWhiteSpace(NewTitle)
            ? GuessTitle(cleanPath)
            : NewTitle.Trim();

        entity.Path = cleanPath;
        entity.ItemType = SelectedItemType;
        entity.Arguments = string.IsNullOrWhiteSpace(NewArguments) ? null : NewArguments.Trim();
        entity.WorkingDirectory = string.IsNullOrWhiteSpace(NewWorkingDirectory) ? null : NewWorkingDirectory.Trim();
        entity.Tags = string.IsNullOrWhiteSpace(NewTags) ? null : NewTags.Trim();
        entity.Note = string.IsNullOrWhiteSpace(NewNote) ? null : NewNote.Trim();
        entity.IconPath = string.IsNullOrWhiteSpace(NewIconPath) ? null : NewIconPath.Trim();
        entity.IsFavorite = NewIsFavorite;
        entity.RunOnAppStart = NewRunOnAppStart;
        entity.ExistsNow = status.ExistsNow;
        entity.PathStatus = status.PathStatus;
        entity.LastCheckedAt = DateTime.Now;
        entity.UpdatedAt = DateTime.Now;

        _db.SaveChanges();

        ClearForm();
        LoadItems();
    }

    [RelayCommand]
    private void EditItem(ManagedItem? item)
    {
        if (item is null)
            return;
        
        IsItemFormExpanded = true;

        var entity = _db.ManagedItems
            .AsNoTracking()
            .FirstOrDefault(x => x.Id == item.Id);

        if (entity is null)
        {
            WpfMessageBox.Show(LocalizationService.Get("Message.ItemNotFound"), "BaroPath");
            LoadItems();
            return;
        }

        EditingItemId = entity.Id;

        NewTitle = entity.Title;
        NewPath = entity.Path;
        SelectedItemType = entity.ItemType;
        NewArguments = entity.Arguments ?? string.Empty;
        NewWorkingDirectory = entity.WorkingDirectory ?? string.Empty;
        NewTags = entity.Tags ?? string.Empty;
        NewNote = entity.Note ?? string.Empty;
        NewIconPath = entity.IconPath ?? string.Empty;
        NewIsFavorite = entity.IsFavorite;
        NewRunOnAppStart = entity.RunOnAppStart;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        ClearForm();
    }

    [RelayCommand]
    private void AddToTargetList(ManagedItem? item)
    {
        if (item is null)
            return;

        if (SelectedTargetCollection is null)
        {
            WpfMessageBox.Show(LocalizationService.Get("Message.SelectTargetList"), "BaroPath");
            return;
        }

        var linked = LinkItemToCollection(item.Id, SelectedTargetCollection.Id);

        if (!linked)
        {
            WpfMessageBox.Show(
                LocalizationService.Format("Message.AlreadyInList", SelectedTargetCollection.Name),
                "BaroPath"
            );
            return;
        }

        WpfMessageBox.Show(
            LocalizationService.Format("Message.AddedToList", SelectedTargetCollection.Name),
            "BaroPath"
        );

        LoadCollections();
        LoadItems();
    }

    [RelayCommand]
    private void MoveToTargetList(ManagedItem? item)
    {
        if (item is null)
            return;

        if (SelectedCollection is null)
        {
            WpfMessageBox.Show(
                LocalizationService.Get("Message.MoveNeedsSource"),
                "BaroPath"
            );
            return;
        }

        if (SelectedTargetCollection is null)
        {
            WpfMessageBox.Show(LocalizationService.Get("Message.SelectTargetList"), "BaroPath");
            return;
        }

        if (SelectedCollection.Id == SelectedTargetCollection.Id)
        {
            WpfMessageBox.Show(LocalizationService.Get("Message.SameList"), "BaroPath");
            return;
        }

        LinkItemToCollection(item.Id, SelectedTargetCollection.Id);

        var oldLink = _db.ItemCollections.FirstOrDefault(x =>
            x.ManagedItemId == item.Id &&
            x.CollectionId == SelectedCollection.Id
        );

        if (oldLink is not null)
        {
            _db.ItemCollections.Remove(oldLink);
            _db.SaveChanges();
        }

        LoadCollections();
        LoadItems();
    }

    [RelayCommand]
    private void RemoveFromCurrentList(ManagedItem? item)
    {
        if (item is null)
            return;

        if (SelectedCollection is null)
        {
            WpfMessageBox.Show(
                LocalizationService.Get("Message.RemoveNeedsList"),
                "BaroPath"
            );
            return;
        }

        var link = _db.ItemCollections.FirstOrDefault(x =>
            x.ManagedItemId == item.Id &&
            x.CollectionId == SelectedCollection.Id
        );

        if (link is null)
        {
            WpfMessageBox.Show(LocalizationService.Get("Message.NotInList"), "BaroPath");
            LoadItems();
            return;
        }

        _db.ItemCollections.Remove(link);
        _db.SaveChanges();

        LoadCollections();
        LoadItems();
    }

    [RelayCommand]
    private void ChooseFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = LocalizationService.Get("Message.ChooseFile"),
            CheckFileExists = true,
            Filter = "All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        NewPath = dialog.FileName;
        SelectedItemType = GuessType(dialog.FileName);

        if (string.IsNullOrWhiteSpace(NewTitle))
            NewTitle = Path.GetFileNameWithoutExtension(dialog.FileName);

        if (string.IsNullOrWhiteSpace(NewWorkingDirectory))
        {
            var dir = Path.GetDirectoryName(dialog.FileName);

            if (!string.IsNullOrWhiteSpace(dir))
                NewWorkingDirectory = dir;
        }
    }

    [RelayCommand]
    private void ChooseFolder()
    {
        
        IsItemFormExpanded = true;
        
        using var dialog = new WinForms.FolderBrowserDialog
        {
            Description = LocalizationService.Get("Message.ChooseFolder"),
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() != WinForms.DialogResult.OK)
            return;

        NewPath = dialog.SelectedPath;
        SelectedItemType = "Folder";

        if (string.IsNullOrWhiteSpace(NewTitle))
            NewTitle = new DirectoryInfo(dialog.SelectedPath).Name;

        if (string.IsNullOrWhiteSpace(NewWorkingDirectory))
            NewWorkingDirectory = dialog.SelectedPath;
    }

    [RelayCommand]
    private void ChooseIcon()
    {
        IsItemFormExpanded = true;

        var dialog = new OpenFileDialog
        {
            Title = LocalizationService.Get("Message.ChooseIcon"),
            Filter = "Images (*.ico;*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.ico;*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
            NewIconPath = dialog.FileName;
    }

    [RelayCommand]
    private void ClearIcon()
    {
        NewIconPath = string.Empty;
    }

    [RelayCommand]
    private void OpenItem(ManagedItem? item)
    {
        if (item is null)
            return;

        try
        {
            ItemLauncher.Open(item);
            TouchItem(item.Id);
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show(ex.Message, LocalizationService.Get("Message.OpenError"));
        }
    }

    [RelayCommand]
    private void OpenInExplorer(ManagedItem? item)
    {
        if (item is null)
            return;

        try
        {
            ItemLauncher.OpenInExplorer(item);
            TouchItem(item.Id);
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show(ex.Message, LocalizationService.Get("Message.ExplorerError"));
        }
    }

    [RelayCommand]
    private void RunItem(ManagedItem? item)
    {
        if (item is null)
            return;

        try
        {
            ItemLauncher.Run(item);
            TouchItem(item.Id);
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show(ex.Message, LocalizationService.Get("Message.RunError"));
        }
    }

    [RelayCommand]
    private void CopyPath(ManagedItem? item)
    {
        if (item is null)
            return;

        WpfClipboard.SetText(item.Path);
    }

    [RelayCommand]
    private void DeleteItem(ManagedItem? item)
    {
        if (item is null)
            return;

        var result = WpfMessageBox.Show(
            LocalizationService.Format("Message.DeleteItemConfirm", item.Title),
            LocalizationService.Get("Message.DeleteItemTitle"),
            WpfMessageBoxButton.YesNo,
            WpfMessageBoxImage.Question
        );

        if (result != WpfMessageBoxResult.Yes)
            return;

        var entity = _db.ManagedItems.FirstOrDefault(x => x.Id == item.Id);

        if (entity is null)
            return;

        _db.ManagedItems.Remove(entity);
        _db.SaveChanges();

        if (EditingItemId == item.Id)
            ClearForm();

        LoadCollections();
        LoadItems();
    }
    
    [RelayCommand]
private void SearchEverything()
{
    EverythingResults.Clear();

    if (string.IsNullOrWhiteSpace(EverythingQuery))
    {
        WpfMessageBox.Show(
            LocalizationService.Get("Message.EnterEverythingQuery"),
            LocalizationService.Get("Title.EverythingSearch"));
        return;
    }

    try
    {
        var results = EverythingSearchService.Search(EverythingQuery, EverythingMaxResults);

        foreach (var result in results)
            EverythingResults.Add(result);

        if (EverythingResults.Count == 0)
            WpfMessageBox.Show(
                LocalizationService.Get("Message.NothingFound"),
                LocalizationService.Get("Title.EverythingSearch"));
    }
    catch (Exception ex)
    {
        WpfMessageBox.Show(ex.Message, "Everything search error");
    }
}

[RelayCommand]
private void FindMovedSelectedItem()
{
    if (SelectedItem is null)
    {
        WpfMessageBox.Show(
            LocalizationService.Get("Message.SelectRecoveryItem"),
            LocalizationService.Get("Title.Recovery"));
        return;
    }

    var oldPath = SelectedItem.Path.TrimEnd('\\', '/');
    var fileName = Path.GetFileName(oldPath);

    if (string.IsNullOrWhiteSpace(fileName))
    {
        WpfMessageBox.Show(
            LocalizationService.Get("Message.OldNameMissing"),
            LocalizationService.Get("Title.Recovery"));
        return;
    }

    EverythingQuery = fileName;
    EverythingResults.Clear();

    try
    {
        var results = EverythingSearchService.Search(fileName, EverythingMaxResults);

        var ordered = results
            .OrderByDescending(x => ScoreRecoveryCandidate(SelectedItem, x))
            .ToList();

        foreach (var result in ordered)
            EverythingResults.Add(result);

        SelectedEverythingResult = EverythingResults.FirstOrDefault();

        if (EverythingResults.Count == 0)
        {
            WpfMessageBox.Show(
                LocalizationService.Format("Message.RecoveryNothing", fileName),
                LocalizationService.Get("Title.Recovery")
            );
            return;
        }

        WpfMessageBox.Show(
            LocalizationService.Format("Message.RecoveryCandidates", EverythingResults.Count),
            LocalizationService.Get("Title.Recovery")
        );
    }
    catch (Exception ex)
    {
        WpfMessageBox.Show(ex.Message, "Recovery error");
    }
}

[RelayCommand]
private void UpdateSelectedItemPathFromEverything()
{
    if (SelectedItem is null)
    {
        WpfMessageBox.Show(
            LocalizationService.Get("Message.SelectManagedItem"),
            LocalizationService.Get("Title.Recovery"));
        return;
    }

    if (SelectedEverythingResult is null)
    {
        WpfMessageBox.Show(
            LocalizationService.Get("Message.SelectEverythingResult"),
            LocalizationService.Get("Title.Recovery"));
        return;
    }

    var newPath = SelectedEverythingResult.Path.Trim();

    if (!File.Exists(newPath) && !Directory.Exists(newPath))
    {
        WpfMessageBox.Show(
            LocalizationService.Get("Message.SelectedPathMissing"),
            LocalizationService.Get("Title.Recovery"));
        return;
    }

    var entity = _db.ManagedItems.FirstOrDefault(x => x.Id == SelectedItem.Id);

    if (entity is null)
    {
        WpfMessageBox.Show(
            LocalizationService.Get("Message.ItemNotFound"),
            LocalizationService.Get("Title.Recovery"));
        LoadItems();
        return;
    }

    var duplicate = _db.ManagedItems.FirstOrDefault(x =>
        x.Id != entity.Id &&
        x.Path == newPath
    );

    if (duplicate is not null)
    {
        WpfMessageBox.Show(
            LocalizationService.Format("Message.DuplicateForOtherItem", duplicate.Title),
            LocalizationService.Get("Title.Recovery")
        );
        return;
    }

    var newItemType = SelectedEverythingResult.ItemType;
    var newWorkingDirectory = SelectedEverythingResult.DirectoryPath;

    var status = EvaluatePathStatus(newPath, newItemType, newWorkingDirectory);

    entity.Path = newPath;
    entity.ItemType = newItemType;
    entity.WorkingDirectory = newWorkingDirectory;
    entity.ExistsNow = status.ExistsNow;
    entity.PathStatus = status.PathStatus;
    entity.LastCheckedAt = DateTime.Now;
    entity.UpdatedAt = DateTime.Now;

    _db.SaveChanges();

    var updatedId = entity.Id;

    LoadItems();

    SelectedItem = Items.FirstOrDefault(x => x.Id == updatedId);

    WpfMessageBox.Show(
        LocalizationService.Get("Message.PathUpdated"),
        LocalizationService.Get("Title.Recovery")
    );
}

private static int ScoreRecoveryCandidate(ManagedItem oldItem, EverythingSearchResult candidate)
{
    var score = 0;

    var oldPath = oldItem.Path.TrimEnd('\\', '/');
    var oldName = Path.GetFileName(oldPath);
    var candidateName = Path.GetFileName(candidate.Path);

    if (string.Equals(oldName, candidateName, StringComparison.OrdinalIgnoreCase))
        score += 100;

    var oldExt = Path.GetExtension(oldPath);
    var candidateExt = Path.GetExtension(candidate.Path);

    if (!string.IsNullOrWhiteSpace(oldExt) &&
        string.Equals(oldExt, candidateExt, StringComparison.OrdinalIgnoreCase))
    {
        score += 25;
    }

    if (string.Equals(oldItem.ItemType, candidate.ItemType, StringComparison.OrdinalIgnoreCase))
        score += 15;

    var oldParent = Path.GetFileName(Path.GetDirectoryName(oldPath) ?? string.Empty);

    if (!string.IsNullOrWhiteSpace(oldParent) &&
        candidate.Path.Contains(oldParent, StringComparison.OrdinalIgnoreCase))
    {
        score += 10;
    }

    return score;
}

[RelayCommand]
private void AddEverythingResult(EverythingSearchResult? result)
{
    if (result is null)
    {
        WpfMessageBox.Show(LocalizationService.Get("Message.SelectEverythingResult"), "BaroPath");
        return;
    }

    if (!File.Exists(result.Path) && !Directory.Exists(result.Path))
    {
        WpfMessageBox.Show(LocalizationService.Get("Message.PathDoesNotExist"), "BaroPath");
        return;
    }

    var cleanPath = result.Path.Trim();

    var existingItem = _db.ManagedItems.FirstOrDefault(x => x.Path == cleanPath);

    if (existingItem is not null)
    {
        if (SelectedCollection is not null)
        {
            var linked = LinkItemToCollection(existingItem.Id, SelectedCollection.Id);

            if (linked)
            {
                WpfMessageBox.Show(
                    LocalizationService.Get("Message.ExistingEverythingAdded"),
                    "BaroPath"
                );

                LoadCollections();
                LoadItems();
                return;
            }
        }

        WpfMessageBox.Show(LocalizationService.Get("Message.AlreadyInManager"), "BaroPath");
        return;
    }

    var itemType = result.ItemType;

    var status = EvaluatePathStatus(cleanPath, itemType, result.DirectoryPath);

    var item = new ManagedItem
    {
        Title = GuessTitle(cleanPath),
        Path = cleanPath,
        ItemType = itemType,
        Arguments = null,
        WorkingDirectory = result.DirectoryPath,
        Tags = null,
        Note = "Added from Everything search",
        IsFavorite = false,
        RunOnAppStart = false,
        ExistsNow = status.ExistsNow,
        PathStatus = status.PathStatus,
        LastCheckedAt = DateTime.Now,
        CreatedAt = DateTime.Now,
        UpdatedAt = DateTime.Now
    };

    _db.ManagedItems.Add(item);
    _db.SaveChanges();

    if (SelectedCollection is not null)
        LinkItemToCollection(item.Id, SelectedCollection.Id);

    LoadCollections();
    LoadItems();

    WpfMessageBox.Show(
        LocalizationService.Get("Message.AddedToManager"),
        LocalizationService.Get("Title.EverythingSearch"));
}

[RelayCommand]
private void OpenEverythingResult(EverythingSearchResult? result)
{
    if (result is null)
        return;

    try
    {
        var item = new ManagedItem
        {
            Title = result.Name,
            Path = result.Path,
            ItemType = result.ItemType,
            WorkingDirectory = result.DirectoryPath
        };

        ItemLauncher.Open(item);
    }
    catch (Exception ex)
    {
        WpfMessageBox.Show(ex.Message, LocalizationService.Get("Message.OpenError"));
    }
}

[RelayCommand]
private void OpenEverythingResultInExplorer(EverythingSearchResult? result)
{
    if (result is null)
        return;

    try
    {
        var item = new ManagedItem
        {
            Title = result.Name,
            Path = result.Path,
            ItemType = result.ItemType,
            WorkingDirectory = result.DirectoryPath
        };

        ItemLauncher.OpenInExplorer(item);
    }
    catch (Exception ex)
    {
        WpfMessageBox.Show(ex.Message, LocalizationService.Get("Message.ExplorerError"));
    }
}

[RelayCommand]
private void CopyEverythingResultPath(EverythingSearchResult? result)
{
    if (result is null)
        return;

    WpfClipboard.SetText(result.Path);
}


public void AddExistingItemToCollection(ManagedItem? item, ManagedCollection? collection)
{
    if (item is null || collection is null)
        return;

    var itemExists = _db.ManagedItems.Any(x => x.Id == item.Id);
    var collectionExists = _db.ManagedCollections.Any(x => x.Id == collection.Id);

    if (!itemExists || !collectionExists)
        return;

    var alreadyLinked = _db.ItemCollections.Any(x =>
        x.ManagedItemId == item.Id &&
        x.CollectionId == collection.Id
    );

    if (alreadyLinked)
        return;

    _db.ItemCollections.Add(new ManagedItemCollection
    {
        ManagedItemId = item.Id,
        CollectionId = collection.Id
    });

    _db.SaveChanges();

    var selectedItemId = item.Id;
    var selectedCollectionId = SelectedCollection?.Id;

    LoadCollections();

    if (selectedCollectionId is not null)
        SelectedCollection = Collections.FirstOrDefault(x => x.Id == selectedCollectionId);

    LoadItems();

    SelectedItem = Items.FirstOrDefault(x => x.Id == selectedItemId);
}

public void RemoveExistingItemFromCurrentCollection(ManagedItem? item)
{
    if (item is null)
        return;

    if (SelectedCollection is null)
    {
        WpfMessageBox.Show(
            LocalizationService.Get("Message.DragAllItems"),
            "Drag & drop"
        );

        return;
    }

    var link = _db.ItemCollections.FirstOrDefault(x =>
        x.ManagedItemId == item.Id &&
        x.CollectionId == SelectedCollection.Id
    );

    if (link is null)
        return;

    _db.ItemCollections.Remove(link);
    _db.SaveChanges();

    LoadCollections();
    LoadItems();
}

    private bool LinkItemToCollection(int itemId, int collectionId)
    {
        var alreadyLinked = _db.ItemCollections.Any(x =>
            x.ManagedItemId == itemId &&
            x.CollectionId == collectionId
        );

        if (alreadyLinked)
            return false;

        _db.ItemCollections.Add(new ManagedItemCollection
        {
            ManagedItemId = itemId,
            CollectionId = collectionId
        });

        _db.SaveChanges();

        return true;
    }

    private void TouchItem(int id)
    {
        var entity = _db.ManagedItems.FirstOrDefault(x => x.Id == id);

        if (entity is null)
            return;

        entity.LastUsedAt = DateTime.Now;
        entity.UpdatedAt = DateTime.Now;

        _db.SaveChanges();

        LoadItems();
    }

    private void ClearForm()
    {
        EditingItemId = null;

        NewTitle = string.Empty;
        NewPath = string.Empty;
        SelectedItemType = "Folder";
        NewArguments = string.Empty;
        NewWorkingDirectory = string.Empty;
        NewTags = string.Empty;
        NewNote = string.Empty;
        NewIconPath = string.Empty;
        NewIsFavorite = false;
        NewRunOnAppStart = false;
        
        IsItemFormExpanded = false;
    }

    private static (bool ExistsNow, string PathStatus) EvaluatePathStatus(ManagedItem item)
    {
        return EvaluatePathStatus(item.Path, item.ItemType, item.WorkingDirectory ?? string.Empty);
    }

    private static (bool ExistsNow, string PathStatus) EvaluatePathStatus(
        string path,
        string itemType,
        string workingDirectory)
    {
        if (itemType == "Command")
            return (true, "Command");

        var pathExists = File.Exists(path) || Directory.Exists(path);

        if (!pathExists)
            return (false, "Missing");

        if (!string.IsNullOrWhiteSpace(workingDirectory) &&
            !Directory.Exists(workingDirectory))
        {
            return (true, "WorkDirMissing");
        }

        return (true, "OK");
    }

    private static string GuessTitle(string path)
    {
        if (Directory.Exists(path))
            return new DirectoryInfo(path).Name;

        var fileName = Path.GetFileNameWithoutExtension(path);

        return string.IsNullOrWhiteSpace(fileName)
            ? path
            : fileName;
    }

    private static string GuessType(string path)
    {
        if (Directory.Exists(path))
            return "Folder";

        var ext = Path.GetExtension(path).ToLowerInvariant();

        return ext switch
        {
            ".exe" => "App",
            ".bat" or ".cmd" or ".ps1" or ".py" => "Script",
            _ => "File"
        };
    }
}
