using System.Collections.ObjectModel;
using System.IO;
using BaroManager.Data;
using BaroManager.Models;
using BaroManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
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

    public ObservableCollection<ManagedCollection> Collections { get; } = new();

    public string[] ItemTypes { get; } =
    [
        "Folder",
        "File",
        "App",
        "Script",
        "Command"
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

    partial void OnSelectedCollectionChanged(ManagedCollection? value)
    {
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

        var collections = _db.ManagedCollections
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList();

        foreach (var collection in collections)
            Collections.Add(collection);

        if (selectedCollectionId is not null)
            SelectedCollection = Collections.FirstOrDefault(x => x.Id == selectedCollectionId.Value);

        if (selectedTargetCollectionId is not null)
            SelectedTargetCollection = Collections.FirstOrDefault(x => x.Id == selectedTargetCollectionId.Value);
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
    private void ShowAll()
    {
        SelectedCollection = null;
        LoadItems();
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

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();

            query = query.Where(x =>
                x.Title.Contains(search) ||
                x.Path.Contains(search) ||
                x.ItemType.Contains(search) ||
                (x.Tags != null && x.Tags.Contains(search)) ||
                (x.Note != null && x.Note.Contains(search))
            );
        }

        var items = query
            .OrderByDescending(x => x.IsFavorite)
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