using System.Windows;
using DVVdownloader.Localization;
using DVVdownloader.Services;

namespace DVVdownloader;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Dil, pencereler oluşturulmadan önce ayarlanmalı; XAML metinleri açılışta okunur
        Loc.Apply(SettingsService.Instance.CurrentSettings.Language);
        base.OnStartup(e);
    }
}
