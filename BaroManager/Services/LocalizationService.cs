using System.Globalization;
using System.Windows;

namespace BaroManager.Services;

public static class LocalizationService
{
    private static readonly IReadOnlyDictionary<string, string> Russian = CreateStrings(useEnglish: false);
    private static readonly IReadOnlyDictionary<string, string> English = CreateStrings(useEnglish: true);
    private static ResourceDictionary? _activeResources;

    public static string CurrentLanguage { get; private set; } = "ru";

    public static string NormalizeLanguage(string? language) =>
        string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "ru";

    public static void ApplyLanguage(string? language)
    {
        CurrentLanguage = NormalizeLanguage(language);

        var culture = CultureInfo.GetCultureInfo(CurrentLanguage == "en" ? "en-US" : "ru-RU");
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        var app = System.Windows.Application.Current;

        if (app is null)
            return;

        if (_activeResources is not null)
            app.Resources.MergedDictionaries.Remove(_activeResources);

        var resources = new ResourceDictionary();

        foreach (var (key, value) in GetCurrentStrings())
            resources[key] = value;

        _activeResources = resources;
        app.Resources.MergedDictionaries.Add(resources);
    }

    public static string Get(string key)
    {
        if (GetCurrentStrings().TryGetValue(key, out var value))
            return value;

        return English.TryGetValue(key, out value) ? value : key;
    }

    public static string Format(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), args);

    private static IReadOnlyDictionary<string, string> GetCurrentStrings() =>
        CurrentLanguage == "en" ? English : Russian;

    private static IReadOnlyDictionary<string, string> CreateStrings(bool useEnglish)
    {
        var strings = new Dictionary<string, string>();

        void Add(string key, string russian, string english) =>
            strings[key] = useEnglish ? english : russian;

        Add("App.Tagline", "локальный мозг для путей / где, блять, файл", "your local path brain / where did that file go?");
        Add("Common.Add", "Добавить", "Add");
        Add("Common.Auto", "Авто", "Auto");
        Add("Common.Cancel", "Отмена", "Cancel");
        Add("Common.Choose", "Выбрать", "Choose");
        Add("Common.Copy", "Копировать", "Copy");
        Add("Common.Delete", "Удалить", "Delete");
        Add("Common.Edit", "Редактировать", "Edit");
        Add("Common.Name", "Название", "Name");
        Add("Common.Open", "Открыть", "Open");
        Add("Common.Path", "Путь", "Path");
        Add("Common.Rename", "Переименовать", "Rename");
        Add("Common.Run", "Запустить", "Run");
        Add("Common.Save", "Сохранить", "Save");
        Add("Common.Test", "Проверить", "Test");
        Add("Common.Type", "Тип", "Type");
        Add("Context.CopyPath", "Копировать путь", "Copy path");
        Add("Context.DeleteEverywhere", "Удалить полностью", "Delete everywhere");
        Add("Context.FindMoved", "Найти переехавший файл", "Find moved file");
        Add("Context.RemoveCurrentList", "Убрать из текущего списка", "Remove from current list");
        Add("Context.ShowInExplorer", "Показать в проводнике", "Show in File Explorer");
        Add("Context.UseEverythingPath", "Взять выбранный путь из Everything", "Use selected Everything path");
        Add("Form.Arguments", "Аргументы:", "Arguments:");
        Add("Form.AutoIconHint", "Убрать свою иконку и использовать автоматическую", "Remove the custom icon and choose one automatically");
        Add("Form.CustomIcon", "Своя иконка:", "Custom icon:");
        Add("Form.CustomIconHint", "Можно оставить пустым — тогда значок возьмётся из файла приложения", "Leave empty to use the application's own icon");
        Add("Form.Favorite", "Избранное", "Favorite");
        Add("Form.File", "Файл", "File");
        Add("Form.Folder", "Папка", "Folder");
        Add("Form.Hint", "  / нажми, чтобы открыть форму добавления или редактирования", "  / click to add or edit an item");
        Add("Form.Name", "Название:", "Name:");
        Add("Form.Note", "Заметка:", "Note:");
        Add("Form.Path", "Путь:", "Path:");
        Add("Form.RunWithManager", "Запускать с менеджером", "Run with BaroPath");
        Add("Form.Tags", "Теги:", "Tags:");
        Add("Form.Type", "Тип:", "Type:");
        Add("Form.WorkingDirectory", "Раб. папка:", "Working dir:");
        Add("Items.CheckPaths", "Проверить пути", "Check paths");
        Add("Items.Filter", "Фильтр:", "Filter:");
        Add("Items.GridView", "▦ Сетка", "▦ Grid");
        Add("Items.InteractionHint", "Двойной клик открывает элемент, правая кнопка показывает меню.", "Double-click to open; right-click for more actions.");
        Add("Items.ListView", "☰ Список", "☰ List");
        Add("Items.OnlyMissing", "Только битые", "Missing only");
        Add("Items.Search", "Поиск:", "Search:");
        Add("Items.SearchHint", "Поиск по названию, пути, типу, статусу, тегам и заметкам", "Search names, paths, types, statuses, tags, and notes");
        Add("Lists.AllItems", "Все элементы", "All items");
        Add("Lists.AllItemsHint", "Показать все элементы. Перетаскивание сюда уберёт элемент из текущего списка.", "Show all items. Drop here to remove an item from the current list.");
        Add("Lists.DropHint", "Перетащи сюда элемент, чтобы добавить его в список.", "Drop an item here to add it to this list.");
        Add("Lists.DragHelp", "Drag & drop: перетащи элемент на список, чтобы добавить. Перетащи на «Все элементы», чтобы убрать из текущего списка.", "Drag & drop: drop an item onto a list to add it. Drop onto All items to remove it from the current list.");
        Add("Lists.New", "Новый список:", "New list:");
        Add("Lists.NewHint", "Название нового списка", "New list name");
        Add("Lists.Selected", "Выбранный список:", "Selected list:");
        Add("Lists.SelectedHint", "Название выбранного списка", "Selected list name");
        Add("Lists.Title", "Списки", "Lists");
        Add("Everything.Add", "Добавить", "Add");
        Add("Everything.Header", "Поиск Everything", "Everything search");
        Add("Everything.MaxResultsHint", "Максимум результатов", "Maximum results");
        Add("Everything.QueryHint", "Запрос для Everything", "Everything search query");
        Add("Everything.Search", "Найти", "Search");
        Add("Grid.Name", "Имя", "Name");
        Add("Grid.Status", "Статус", "Status");
        Add("Grid.Tags", "Теги", "Tags");
        Add("Settings.AutoStartEverything", "Автоматически запускать встроенный Everything", "Automatically start embedded Everything");
        Add("Settings.DatabaseFolder", "Открыть папку базы", "Open database folder");
        Add("Settings.EverythingPathHint", "Путь к Everything.exe", "Path to Everything.exe");
        Add("Settings.EsPathHint", "Путь к es.exe", "Path to es.exe");
        Add("Settings.ExportJson", "Экспорт JSON", "Export JSON");
        Add("Settings.ImportJson", "Импорт JSON", "Import JSON");
        Add("Settings.Language", "Язык интерфейса", "Interface language");
        Add("Settings.Maintenance", "Обслуживание", "Maintenance");
        Add("Settings.SettingsFolder", "Папка настроек", "Settings folder");
        Add("Settings.Title", "Настройки", "Settings");
        Add("Settings.ToolsFolder", "Папка tools", "Tools folder");
        Add("Sidebar.Actions", "Действия", "Actions");
        Add("Sidebar.FindMoved", "Найти переезд", "Find moved");
        Add("Sidebar.Note", "Заметка", "Note");
        Add("Sidebar.NoneSelected", "Ничего не выбрано", "Nothing selected");
        Add("Sidebar.RemoveList", "Убрать из списка", "Remove from list");
        Add("Sidebar.SelectedItem", "Выбранный элемент", "Selected item");
        Add("Sidebar.UseEsPath", "Взять путь ES", "Use ES path");
        Add("Sidebar.WorkingDirectory", "Рабочая папка", "Working directory");
        Add("Type.Folder", "📁 Папка", "📁 Folder");
        Add("Type.File", "📄 Файл", "📄 File");
        Add("Type.App", "🚀 Приложение", "🚀 Application");
        Add("Type.Script", "⚙ Скрипт", "⚙ Script");
        Add("Type.Command", "⌨ Команда", "⌨ Command");
        Add("Status.OK", "✅ OK", "✅ OK");
        Add("Status.Missing", "❌ Не найдено", "❌ Missing");
        Add("Status.WorkDirMissing", "⚠ Нет рабочей папки", "⚠ Working directory missing");
        Add("Status.Command", "⌨ Команда", "⌨ Command");
        Add("Status.Unknown", "❔ Неизвестно", "❔ Unknown");
        Add("Filter.All", "Все", "All");
        Add("Filter.Favorites", "Избранное", "Favorites");
        Add("Filter.Missing", "Отсутствующие", "Missing");
        Add("Filter.Commands", "Команды", "Commands");
        Add("Filter.Startup", "Автозапуск", "Startup");
        Add("Filter.Folders", "Папки", "Folders");
        Add("Filter.Scripts", "Скрипты", "Scripts");
        Add("Filter.Apps", "Приложения", "Applications");
        Add("Action.Add", "Добавить", "Add");
        Add("Action.Save", "Сохранить", "Save");
        Add("Form.NewItem", "Новый элемент", "New item");
        Add("Form.EditItem", "Редактирование элемента", "Edit item");
        Add("Message.ListNameEmpty", "Название списка пустое.", "The list name is empty.");
        Add("Message.ListExists", "Такой список уже есть.", "A list with this name already exists.");
        Add("Message.SettingsSaved", "Настройки сохранены.", "Settings saved.");
        Add("Message.ChooseEs", "Выбери es.exe", "Choose es.exe");
        Add("Message.ChooseEverything", "Выбери Everything.exe", "Choose Everything.exe");
        Add("Message.EverythingWorks", "Everything работает.\n\nes.exe:\n{0}\n\nEverything.exe:\n{1}\n\nТестовых результатов: {2}", "Everything is working.\n\nes.exe:\n{0}\n\nEverything.exe:\n{1}\n\nTest results: {2}");
        Add("Message.ChooseBackupSave", "Сохранить резервную копию BaroPath", "Save a BaroPath backup");
        Add("Message.BackupSaved", "Резервная копия сохранена:\n\n{0}", "Backup saved:\n\n{0}");
        Add("Message.ChooseBackupOpen", "Выбери резервную копию BaroPath", "Choose a BaroPath backup");
        Add("Message.ImportConfirm", "Импорт объединит данные с текущей базой.\n\nСуществующие пути будут обновлены, новые — добавлены.\nНичего автоматически удаляться не будет.\n\nПродолжить?", "The import will merge data with the current database.\n\nExisting paths will be updated and new paths will be added.\nNothing will be deleted automatically.\n\nContinue?");
        Add("Message.ImportComplete", "Импорт завершён.\n\nСписков создано: {0}\nЭлементов создано: {1}\nЭлементов обновлено: {2}\nСвязей со списками создано: {3}", "Import complete.\n\nLists created: {0}\nItems created: {1}\nItems updated: {2}\nList links created: {3}");
        Add("Message.SelectListLeft", "Сначала выбери список слева.", "Select a list on the left first.");
        Add("Message.ListDuplicate", "Список с таким названием уже есть.", "A list with this name already exists.");
        Add("Message.ListNotFound", "Список не найден в базе.", "The list was not found in the database.");
        Add("Message.DeleteListConfirm", "Удалить список?\n\n{0}\n\nЭлементы из менеджера не удалятся. Удалится только список и его привязки.", "Delete this list?\n\n{0}\n\nItems will remain in BaroPath; only the list and its links will be deleted.");
        Add("Message.DeleteListTitle", "Удаление списка", "Delete list");
        Add("Message.ListAlreadyGone", "Список уже не найден в базе.", "The list no longer exists in the database.");
        Add("Message.CheckComplete", "Проверка завершена.\n\nOK: {0}\nНе найдено: {1}\nНет рабочей папки: {2}\nКоманд: {3}", "Check complete.\n\nOK: {0}\nMissing: {1}\nWorking directory missing: {2}\nCommands: {3}");
        Add("Message.PathEmpty", "Путь пустой.", "The path is empty.");
        Add("Message.PathMissingConfirm", "Такой путь сейчас не существует. Всё равно сохранить?", "This path does not currently exist. Save it anyway?");
        Add("Message.PathMissingTitle", "Путь не найден", "Path not found");
        Add("Message.ExistingAddedToList", "Этот путь уже был сохранён, поэтому он добавлен в выбранный список.", "This path was already saved, so it was added to the selected list.");
        Add("Message.PathDuplicate", "Такой путь уже сохранён.", "This path is already saved.");
        Add("Message.ItemNotFound", "Элемент не найден в базе.", "The item was not found in the database.");
        Add("Message.OtherItemDuplicate", "Другой элемент уже использует такой путь.", "Another item already uses this path.");
        Add("Message.SelectTargetList", "Сначала выбери целевой список.", "Select a target list first.");
        Add("Message.AlreadyInList", "Элемент уже есть в списке «{0}».", "The item is already in “{0}”.");
        Add("Message.AddedToList", "Добавлено в список «{0}».", "Added to “{0}”.");
        Add("Message.MoveNeedsSource", "Для переноса сначала выбери исходный список слева.", "Select the source list on the left before moving an item.");
        Add("Message.SameList", "Исходный и целевой список совпадают.", "The source and target lists are the same.");
        Add("Message.RemoveNeedsList", "Сначала выбери конкретный список слева.", "Select a specific list on the left first.");
        Add("Message.NotInList", "Этого элемента нет в выбранном списке.", "This item is not in the selected list.");
        Add("Message.ChooseFile", "Выбери файл, программу или скрипт", "Choose a file, application, or script");
        Add("Message.ChooseFolder", "Выбери папку", "Choose a folder");
        Add("Message.ChooseIcon", "Выбери свою иконку", "Choose a custom icon");
        Add("Message.OpenError", "Ошибка открытия", "Open error");
        Add("Message.ExplorerError", "Ошибка проводника", "File Explorer error");
        Add("Message.RunError", "Ошибка запуска", "Run error");
        Add("Message.DeleteItemConfirm", "Удалить элемент из менеджера?\n\n{0}\n\nФайл на диске не удаляется.", "Delete this item from BaroPath?\n\n{0}\n\nThe file on disk will not be deleted.");
        Add("Message.DeleteItemTitle", "Глобальное удаление", "Delete item");
        Add("Message.EnterEverythingQuery", "Введите запрос для Everything.", "Enter an Everything search query.");
        Add("Message.NothingFound", "Ничего не найдено.", "Nothing found.");
        Add("Message.SelectRecoveryItem", "Сначала выбери элемент, который нужно найти.", "Select the item you want to find first.");
        Add("Message.OldNameMissing", "Не удалось получить имя файла или папки из старого пути.", "Could not get a file or folder name from the old path.");
        Add("Message.RecoveryNothing", "По имени «{0}» ничего не найдено.", "Nothing named “{0}” was found.");
        Add("Message.RecoveryCandidates", "Найдено кандидатов: {0}\n\nВыбери подходящий результат и нажми «Взять путь ES».", "Candidates found: {0}\n\nSelect the correct result and click Use ES path.");
        Add("Message.SelectManagedItem", "Сначала выбери элемент BaroPath.", "Select a BaroPath item first.");
        Add("Message.SelectEverythingResult", "Сначала выбери результат Everything.", "Select an Everything result first.");
        Add("Message.SelectedPathMissing", "Выбранный путь уже не существует.", "The selected path no longer exists.");
        Add("Message.DuplicateForOtherItem", "Такой путь уже есть у другого элемента:\n\n{0}", "Another item already uses this path:\n\n{0}");
        Add("Message.PathUpdated", "Путь обновлён из Everything.", "The path was updated from Everything.");
        Add("Message.PathDoesNotExist", "Этот путь уже не существует.", "This path no longer exists.");
        Add("Message.ExistingEverythingAdded", "Этот путь уже был в менеджере, поэтому он добавлен в выбранный список.", "This path was already in BaroPath, so it was added to the selected list.");
        Add("Message.AlreadyInManager", "Такой путь уже есть в BaroPath.", "This path is already in BaroPath.");
        Add("Message.AddedToManager", "Добавлено в BaroPath.", "Added to BaroPath.");
        Add("Message.DragAllItems", "Сначала выбери конкретный список слева.\n\nИз режима «Все элементы» убирать нечего.", "Select a specific list on the left first.\n\nThere is nothing to remove in All items mode.");
        Add("Message.BackupNotFound", "Файл резервной копии не найден.", "The backup file was not found.");
        Add("Message.BackupReadError", "Не удалось прочитать файл резервной копии.", "The backup file could not be read.");
        Add("Message.PathNotFound", "Путь не найден.", "Path not found.");
        Add("Message.StartupProblems", "Некоторые элементы автозапуска не были запущены.", "Some startup items could not be launched.");
        Add("Message.Total", "Всего: {0}", "Total: {0}");
        Add("Message.Started", "Запущено: {0}", "Started: {0}");
        Add("Message.Skipped", "Пропущено: {0}", "Skipped: {0}");
        Add("Message.Errors", "Ошибок: {0}", "Errors: {0}");
        Add("Message.SkippedHeader", "Пропущено:", "Skipped:");
        Add("Message.ErrorsHeader", "Ошибки:", "Errors:");
        Add("Message.MissingPathSuffix", "путь отсутствует", "path is missing");
        Add("Message.MissingWorkDirSuffix", "рабочая папка отсутствует", "working directory is missing");
        Add("Message.EsMissing", "Не найден встроенный es.exe.", "The embedded es.exe was not found.");
        Add("Message.EsExitCode", "es.exe завершился с кодом {0}.", "es.exe exited with code {0}.");
        Add("Message.EverythingUnavailable", "Встроенный Everything не запущен или ещё создаёт индекс. Повтори поиск через несколько секунд.", "Embedded Everything is not running or is still building its index. Try again in a few seconds.");
        Add("Message.EsStartFailed", "Не удалось запустить es.exe.", "Could not start es.exe.");
        Add("Message.EverythingTimeout", "Everything слишком долго отвечает. Возможно, индекс ещё создаётся.", "Everything took too long to respond. Its index may still be building.");
        Add("Message.EmbeddedEverythingMissing", "Не найден встроенный Everything.exe.", "The embedded Everything.exe was not found.");
        Add("Title.Settings", "Настройки", "Settings");
        Add("Title.Backup", "Резервная копия", "Backup");
        Add("Title.ImportBackup", "Импорт резервной копии", "Import backup");
        Add("Title.CheckPaths", "Проверка путей", "Check paths");
        Add("Title.EverythingSearch", "Поиск Everything", "Everything search");
        Add("Title.Recovery", "Восстановление", "Recovery");
        Add("Title.StartupItems", "Автозапуск", "Startup items");

        return strings;
    }
}
