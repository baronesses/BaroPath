using System.Windows;
using BaroManager.Data;
using BaroManager.Services;
using BaroManager.ViewModels;

namespace BaroManager;

public partial class MainWindow : Window
{
    private readonly AppDbContext _db;

    public MainWindow()
    {
        InitializeComponent();

        _db = new AppDbContext();

        DatabaseInitializer.Initialize(_db);

        DataContext = new MainViewModel(_db);
        StartupItemRunner.RunStartupItems(_db);
    }

    protected override void OnClosed(EventArgs e)
    {
        _db.Dispose();
        base.OnClosed(e);
    }
}