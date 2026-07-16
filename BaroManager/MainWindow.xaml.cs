using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BaroManager.Data;
using BaroManager.Models;
using BaroManager.Services;
using BaroManager.ViewModels;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;
using WpfPoint = System.Windows.Point;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfDragEventArgs = System.Windows.DragEventArgs;
using WpfDragDropEffects = System.Windows.DragDropEffects;
using WpfKey = System.Windows.Input.Key;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfListBox = System.Windows.Controls.ListBox;

namespace BaroManager;

public partial class MainWindow : System.Windows.Window
{
    private AppDbContext? _db;
    private bool _startupItemsAlreadyRan;
    private WpfPoint _dragStartPoint;

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
                "Database error",
                WpfMessageBoxButton.OK,
                WpfMessageBoxImage.Error
            );

            Close();
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_startupItemsAlreadyRan || _db is null)
            return;

        _startupItemsAlreadyRan = true;

        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

        var result = StartupItemRunner.RunStartupItems(_db);

        if (result.Total == 0 || !result.HasProblems)
            return;

        var message = BuildStartupMessage(result);

        WpfMessageBox.Show(
            message,
            "Startup items",
            WpfMessageBoxButton.OK,
            WpfMessageBoxImage.Warning
        );
    }

    private static string BuildStartupMessage(StartupRunResult result)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Некоторые элементы автозапуска не были запущены.");
        builder.AppendLine();
        builder.AppendLine($"Всего: {result.Total}");
        builder.AppendLine($"Запущено: {result.Started.Count}");
        builder.AppendLine($"Пропущено: {result.Skipped.Count}");
        builder.AppendLine($"Ошибок: {result.Failed.Count}");

        if (result.Skipped.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Пропущено:");

            foreach (var skipped in result.Skipped.Take(10))
                builder.AppendLine($"- {skipped}");
        }

        if (result.Failed.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Ошибки:");

            foreach (var failed in result.Failed.Take(10))
                builder.AppendLine($"- {failed}");
        }

        return builder.ToString();
    }

    private void MainItemsGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private void MainItemsGrid_MouseMove(object sender, WpfMouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        var currentPosition = e.GetPosition(null);

        var diff = _dragStartPoint - currentPosition;

        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var container = FindManagedItemContainer((DependencyObject)e.OriginalSource);

        if (container?.DataContext is not ManagedItem item)
            return;

        DragDrop.DoDragDrop(container, item, WpfDragDropEffects.Copy | WpfDragDropEffects.Move);
    }

    private void MainItems_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var container = FindManagedItemContainer((DependencyObject)e.OriginalSource);

        if (container?.DataContext is not ManagedItem item)
            return;

        switch (sender)
        {
            case DataGrid dataGrid:
                dataGrid.SelectedItem = item;
                break;

            case WpfListBox listBox:
                listBox.SelectedItem = item;
                break;
        }

        container.Focus();
    }

    private void MainItemsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var container = FindManagedItemContainer((DependencyObject)e.OriginalSource);

        if (container?.DataContext is not ManagedItem item)
            return;

        if (DataContext is not MainViewModel viewModel)
            return;

        if (viewModel.OpenItemCommand.CanExecute(item))
            viewModel.OpenItemCommand.Execute(item);
    }

    private void MainItemsGrid_KeyDown(object sender, WpfKeyEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
            return;

        var item = sender switch
        {
            DataGrid dataGrid => dataGrid.SelectedItem as ManagedItem,
            WpfListBox listBox => listBox.SelectedItem as ManagedItem,
            _ => null
        };

        if (item is null)
            return;

        switch (e.Key)
        {
            case WpfKey.Enter:
                if (viewModel.OpenItemCommand.CanExecute(item))
                    viewModel.OpenItemCommand.Execute(item);

                e.Handled = true;
                break;

            case WpfKey.F2:
                if (viewModel.EditItemCommand.CanExecute(item))
                    viewModel.EditItemCommand.Execute(item);

                e.Handled = true;
                break;

            case WpfKey.C when Keyboard.Modifiers.HasFlag(ModifierKeys.Control):
                if (viewModel.CopyPathCommand.CanExecute(item))
                    viewModel.CopyPathCommand.Execute(item);

                e.Handled = true;
                break;
        }
    }

    private static FrameworkElement? FindManagedItemContainer(DependencyObject source)
    {
        var dataGridRow = FindParent<DataGridRow>(source);

        if (dataGridRow is not null)
            return dataGridRow;

        return FindParent<ListBoxItem>(source);
    }

    private void CollectionListBox_DragOver(object sender, WpfDragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(ManagedItem)))
            e.Effects = WpfDragDropEffects.Copy;
        else
            e.Effects = WpfDragDropEffects.None;

        e.Handled = true;
    }

    private void CollectionListBox_Drop(object sender, WpfDragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(ManagedItem)))
            return;

        if (DataContext is not MainViewModel viewModel)
            return;

        var item = e.Data.GetData(typeof(ManagedItem)) as ManagedItem;

        var listBoxItem = FindParent<ListBoxItem>((DependencyObject)e.OriginalSource);

        if (listBoxItem?.DataContext is not ManagedCollection collection)
            return;

        viewModel.AddExistingItemToCollection(item, collection);

        e.Handled = true;
    }

    private void AllItemsButton_DragOver(object sender, WpfDragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(ManagedItem)))
            e.Effects = WpfDragDropEffects.Move;
        else
            e.Effects = WpfDragDropEffects.None;

        e.Handled = true;
    }

    private void AllItemsButton_Drop(object sender, WpfDragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(ManagedItem)))
            return;

        if (DataContext is not MainViewModel viewModel)
            return;

        var item = e.Data.GetData(typeof(ManagedItem)) as ManagedItem;

        viewModel.RemoveExistingItemFromCurrentCollection(item);

        e.Handled = true;
    }

    private static T? FindParent<T>(DependencyObject? child)
        where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T parent)
                return parent;

            child = VisualTreeHelper.GetParent(child);
        }

        return null;
    }

    protected override void OnClosed(EventArgs e)
    {
        Loaded -= MainWindow_Loaded;

        _db?.Dispose();

        base.OnClosed(e);
    }
}
