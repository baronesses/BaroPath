using System.Windows;
using System.Windows.Threading;
using BaroManager.Data;
using BaroManager.Services;
using BaroManager.ViewModels;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;

namespace BaroManager;

public partial class MainWindow : System.Windows.Window
{
    private AppDbContext? _db;
    private bool _startupItemsAlreadyRan;

    public MainWindow()
    {
        InitializeComponent();

        try
        {
            _db = new AppDbContext();

            DatabaseInitializer.Initialize(_db);

            DataContext = new MainViewModel(_db);

            Loaded += MainWindow_Loaded;
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show(
                ex.Message,
                "Ошибка запуска BaroManager",
                WpfMessageBoxButton.OK,
                WpfMessageBoxImage.Error
            );

            Close();
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_startupItemsAlreadyRan)
            return;

        if (_db is null)
            return;

        _startupItemsAlreadyRan = true;

        // Даём окну нормально открыться, чтобы автозапуск не ощущался как внезапный удар табуреткой.
        await Dispatcher.InvokeAsync(
            () => { },
            DispatcherPriority.ApplicationIdle
        );

        var result = StartupItemRunner.RunStartupItems(_db);

        if (result.Total == 0)
            return;

        if (!result.HasProblems)
            return;

        var message =
            $"Автозапуск завершён с нюансами.\n\n" +
            $"Всего: {result.Total}\n" +
            $"Запущено: {result.Started.Count}\n" +
            $"Пропущено: {result.Skipped.Count}\n" +
            $"Ошибок: {result.Failed.Count}";

        if (result.Skipped.Count > 0)
        {
            message += "\n\nПропущено:\n";
            message += string.Join("\n", result.Skipped.Take(10));

            if (result.Skipped.Count > 10)
                message += $"\n...и ещё {result.Skipped.Count - 10}";
        }

        if (result.Failed.Count > 0)
        {
            message += "\n\nОшибки:\n";
            message += string.Join("\n", result.Failed.Take(10));

            if (result.Failed.Count > 10)
                message += $"\n...и ещё {result.Failed.Count - 10}";
        }

        WpfMessageBox.Show(
            message,
            "Startup items",
            WpfMessageBoxButton.OK,
            WpfMessageBoxImage.Warning
        );
    }

    protected override void OnClosed(EventArgs e)
    {
        Loaded -= MainWindow_Loaded;

        _db?.Dispose();

        base.OnClosed(e);
    }
}