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

    public string[] ItemTypes { get; } =
    [
        "Folder",
        "File",
        "App",
        "Script",
        "Command"
    ];
    
    public string[] FilterModes { get; } =
    [
        "All",
        "Favorites",
        "Missing",
        "Commands",
        "Startup",
        "Folders",
        "Scripts",
        "Apps"
    ];

    public string MainActionText => EditingItemId is null
        ? "Добавить"
        : "Сохранить";

    public string FormTitleText => EditingItemId is null
        ? "Новый элемент"
        : "Редактирование элемента";

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
        LoadItems();
    }

    partial void OnEditingItemIdChanged(int? value)
    {
        OnPropertyChanged(nameof(MainActionText));
        OnPropertyChanged(nameof(FormTitleText));
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
            WpfMessageBox.Show("Название списка пустое.", "BaroManager");
            return;
        }

        var name = NewCollectionName.Trim();

        if (_db.ManagedCollections.Any(x => x.Name == name))
        {
            WpfMessageBox.Show("Такой список уже есть.", "BaroManager");
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
    
    [RelayCommand]
private void ExportBackup()
{
    var dialog = new SaveFileDialog
    {
        Title = "Сохранить backup BaroManager",
        Filter = "BaroManager backup (*.json)|*.json|JSON (*.json)|*.json|All files (*.*)|*.*",
        FileName = $"baromanager-backup-{DateTime.Now:yyyyMMdd-HHmmss}.json"
    };

    if (dialog.ShowDialog() != true)
        return;

    try
    {
        BackupService.Export(_db, dialog.FileName);

        WpfMessageBox.Show(
            $"Backup сохранён:\n\n{dialog.FileName}",
            "Backup"
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
        Title = "Выбери backup BaroManager",
        Filter = "BaroManager backup (*.json)|*.json|JSON (*.json)|*.json|All files (*.*)|*.*",
        CheckFileExists = true
    };

    if (dialog.ShowDialog() != true)
        return;

    var answer = WpfMessageBox.Show(
        "Импорт объединит данные с текущей базой.\n\n" +
        "Существующие пути будут обновлены, новые — добавлены.\n" +
        "Ничего автоматически удаляться не будет.\n\n" +
        "Продолжить?",
        "Import backup",
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
            "Импорт завершён.\n\n" +
            $"Списков создано: {result.CollectionsCreated}\n" +
            $"Элементов создано: {result.ItemsCreated}\n" +
            $"Элементов обновлено: {result.ItemsUpdated}\n" +
            $"Связей со списками создано: {result.LinksCreated}",
            "Import backup"
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
            WpfMessageBox.Show("Сначала выбери список слева.", "BaroManager");
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedCollectionName))
        {
            WpfMessageBox.Show("Название списка пустое.", "BaroManager");
            return;
        }

        var newName = SelectedCollectionName.Trim();

        var duplicate = _db.ManagedCollections.Any(x =>
            x.Id != SelectedCollection.Id &&
            x.Name == newName
        );

        if (duplicate)
        {
            WpfMessageBox.Show("Список с таким названием уже есть.", "BaroManager");
            return;
        }

        var entity = _db.ManagedCollections.FirstOrDefault(x => x.Id == SelectedCollection.Id);

        if (entity is null)
        {
            WpfMessageBox.Show("Список не найден в базе.", "BaroManager");
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
            WpfMessageBox.Show("Сначала выбери список слева.", "BaroManager");
            return;
        }

        var result = WpfMessageBox.Show(
            $"Удалить список?\n\n{SelectedCollection.Name}\n\nЭлементы из менеджера НЕ удалятся. Удалится только сам список и его привязки.",
            "Удаление списка",
            WpfMessageBoxButton.YesNo,
            WpfMessageBoxImage.Question
        );

        if (result != WpfMessageBoxResult.Yes)
            return;

        var collectionId = SelectedCollection.Id;

        var entity = _db.ManagedCollections.FirstOrDefault(x => x.Id == collectionId);

        if (entity is null)
        {
            WpfMessageBox.Show("Список уже не найден в базе.", "BaroManager");
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
            $"Проверка завершена.\n\nOK: {ok}\nMissing: {missing}\nWork dir missing: {workDirMissing}\nCommand: {commands}",
            "Check paths"
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
            WpfMessageBox.Show("Путь пустой. Ну камон.", "BaroManager");
            return;
        }

        var cleanPath = NewPath.Trim();

        if (SelectedItemType != "Command" &&
            !File.Exists(cleanPath) &&
            !Directory.Exists(cleanPath))
        {
            var result = WpfMessageBox.Show(
                "Такой путь сейчас не существует. Всё равно сохранить?",
                "Путь не найден",
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
                        "Этот путь уже был сохранён, поэтому я просто добавил его в выбранный список.",
                        "BaroManager"
                    );

                    ClearForm();
                    LoadCollections();
                    LoadItems();
                    return;
                }
            }

            WpfMessageBox.Show("Такой путь уже сохранён.", "BaroManager");
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
            WpfMessageBox.Show("Путь пустой. Ну камон.", "BaroManager");
            return;
        }

        var cleanPath = NewPath.Trim();

        if (SelectedItemType != "Command" &&
            !File.Exists(cleanPath) &&
            !Directory.Exists(cleanPath))
        {
            var result = WpfMessageBox.Show(
                "Такой путь сейчас не существует. Всё равно сохранить?",
                "Путь не найден",
                WpfMessageBoxButton.YesNo,
                WpfMessageBoxImage.Warning
            );

            if (result != WpfMessageBoxResult.Yes)
                return;
        }

        var entity = _db.ManagedItems.FirstOrDefault(x => x.Id == EditingItemId.Value);

        if (entity is null)
        {
            WpfMessageBox.Show("Элемент не найден в базе.", "BaroManager");
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
                "Другой элемент уже использует такой путь. Дубль не сохраняю.",
                "BaroManager"
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
            WpfMessageBox.Show("Элемент не найден в базе.", "BaroManager");
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
            WpfMessageBox.Show("Сначала выбери целевой список сверху.", "BaroManager");
            return;
        }

        var linked = LinkItemToCollection(item.Id, SelectedTargetCollection.Id);

        if (!linked)
        {
            WpfMessageBox.Show(
                $"Элемент уже есть в списке \"{SelectedTargetCollection.Name}\".",
                "BaroManager"
            );
            return;
        }

        WpfMessageBox.Show(
            $"Добавлено в список \"{SelectedTargetCollection.Name}\".",
            "BaroManager"
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
                "Для переноса сначала выбери исходный список слева. Из режима 'Все элементы' переносить опасно и непонятно откуда.",
                "BaroManager"
            );
            return;
        }

        if (SelectedTargetCollection is null)
        {
            WpfMessageBox.Show("Сначала выбери целевой список сверху.", "BaroManager");
            return;
        }

        if (SelectedCollection.Id == SelectedTargetCollection.Id)
        {
            WpfMessageBox.Show("Исходный и целевой список одинаковые. Оно уже там, капитан.", "BaroManager");
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
                "Сначала выбери конкретный список слева. Из режима 'Все элементы' удалять из списка нечего.",
                "BaroManager"
            );
            return;
        }

        var link = _db.ItemCollections.FirstOrDefault(x =>
            x.ManagedItemId == item.Id &&
            x.CollectionId == SelectedCollection.Id
        );

        if (link is null)
        {
            WpfMessageBox.Show("Этого элемента и так нет в выбранном списке.", "BaroManager");
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
            Title = "Выбери файл / программу / скрипт",
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
            Description = "Выбери папку",
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
            WpfMessageBox.Show(ex.Message, "Ошибка открытия");
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
            WpfMessageBox.Show(ex.Message, "Ошибка Explorer");
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
            WpfMessageBox.Show(ex.Message, "Ошибка запуска");
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
            $"Удалить элемент ВЕЗДЕ из менеджера?\n\n{item.Title}\n\nФайл на диске не удаляется, только запись из BaroManager.",
            "Глобальное удаление",
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
        WpfMessageBox.Show("Введите запрос для Everything.", "Everything search");
        return;
    }

    try
    {
        var results = EverythingSearchService.Search(EverythingQuery, EverythingMaxResults);

        foreach (var result in results)
            EverythingResults.Add(result);

        if (EverythingResults.Count == 0)
            WpfMessageBox.Show("Ничего не найдено.", "Everything search");
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
        WpfMessageBox.Show("Сначала выбери элемент, который надо найти.", "Recovery");
        return;
    }

    var oldPath = SelectedItem.Path.TrimEnd('\\', '/');
    var fileName = Path.GetFileName(oldPath);

    if (string.IsNullOrWhiteSpace(fileName))
    {
        WpfMessageBox.Show("Не удалось вытащить имя файла/папки из старого пути.", "Recovery");
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
                $"По имени \"{fileName}\" ничего не найдено.",
                "Recovery"
            );
            return;
        }

        WpfMessageBox.Show(
            $"Найдено кандидатов: {EverythingResults.Count}\n\nВыбери подходящий результат в Everything search и нажми Use ES path.",
            "Recovery"
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
        WpfMessageBox.Show("Сначала выбери элемент BaroManager.", "Recovery");
        return;
    }

    if (SelectedEverythingResult is null)
    {
        WpfMessageBox.Show("Сначала выбери результат Everything.", "Recovery");
        return;
    }

    var newPath = SelectedEverythingResult.Path.Trim();

    if (!File.Exists(newPath) && !Directory.Exists(newPath))
    {
        WpfMessageBox.Show("Выбранный путь уже не существует.", "Recovery");
        return;
    }

    var entity = _db.ManagedItems.FirstOrDefault(x => x.Id == SelectedItem.Id);

    if (entity is null)
    {
        WpfMessageBox.Show("Элемент не найден в базе.", "Recovery");
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
            $"Такой путь уже есть у другого элемента:\n\n{duplicate.Title}",
            "Recovery"
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
        "Путь обновлён из Everything.",
        "Recovery"
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
        WpfMessageBox.Show("Сначала выбери результат Everything.", "BaroManager");
        return;
    }

    if (!File.Exists(result.Path) && !Directory.Exists(result.Path))
    {
        WpfMessageBox.Show("Этот путь уже не существует.", "BaroManager");
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
                    "Этот путь уже был в менеджере, поэтому я просто добавил его в выбранный список.",
                    "BaroManager"
                );

                LoadCollections();
                LoadItems();
                return;
            }
        }

        WpfMessageBox.Show("Такой путь уже есть в BaroManager.", "BaroManager");
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

    WpfMessageBox.Show("Добавлено в BaroManager.", "Everything search");
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
        WpfMessageBox.Show(ex.Message, "Ошибка открытия");
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
        WpfMessageBox.Show(ex.Message, "Ошибка Explorer");
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
            "Сначала выбери конкретный список слева.\n\n" +
            "Из режима «Все элементы» вытаскивать нечего.",
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