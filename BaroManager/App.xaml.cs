namespace BaroManager;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        var settings = Services.AppSettingsService.Load();
        Services.LocalizationService.ApplyLanguage(settings.Language);

        base.OnStartup(e);
    }
}
