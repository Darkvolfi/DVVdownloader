using System;
using System.Globalization;
using System.Resources;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace DVVdownloader.Localization
{
    /// <summary>
    /// Arayüz metinleri Resources/Strings*.resx dosyalarından okunur.
    /// Strings.resx İngilizcedir ve sistem dili desteklenmiyorsa otomatik olarak kullanılır.
    /// </summary>
    public static class Loc
    {
        private static readonly ResourceManager s_resources =
            new("DVVdownloader.Resources.Strings", typeof(Loc).Assembly);

        // Dil kodu -> kendi dilindeki adı. Boş kod = sistem dili.
        public static readonly (string Code, string NativeName)[] SupportedLanguages =
        {
            ("en", "English"),
            ("tr", "Türkçe"),
            ("de", "Deutsch"),
            ("es", "Español"),
            ("fr", "Français"),
            ("pt", "Português"),
            ("ru", "Русский"),
            ("ar", "العربية"),
            ("id", "Bahasa Indonesia"),
            ("hi", "हिन्दी"),
            ("ja", "日本語"),
            ("zh-Hans", "简体中文"),
        };

        public static string Get(string key) =>
            s_resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;

        public static string Format(string key, params object[] args) =>
            string.Format(CultureInfo.CurrentCulture, Get(key), args);

        /// <summary>
        /// Uygulama açılırken bir kez çağrılır. Boş kod sistemin arayüz dilini kullanır.
        /// </summary>
        public static void Apply(string? languageCode)
        {
            // Sistem dili = kullanıcının Windows görüntüleme dili
            CultureInfo ui = string.IsNullOrWhiteSpace(languageCode)
                ? CultureInfo.CurrentUICulture
                : CultureInfo.GetCultureInfo(languageCode);

            CultureInfo.DefaultThreadCurrentUICulture = ui;
            Thread.CurrentThread.CurrentUICulture = ui;

            // Tarih/sayı biçimleri de seçilen dile uysun (WPF bağlamaları varsayılan olarak en-US kullanır)
            if (!ui.IsNeutralCulture)
            {
                CultureInfo.DefaultThreadCurrentCulture = ui;
                Thread.CurrentThread.CurrentCulture = ui;
            }
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(ui.IetfLanguageTag)));
        }

        public static bool IsRightToLeft => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
    }

    /// <summary>XAML kısayolu: Text="{l:Loc AnahtarAdı}"</summary>
    [MarkupExtensionReturnType(typeof(string))]
    public class LocExtension : MarkupExtension
    {
        public LocExtension() { }
        public LocExtension(string key) { Key = key; }

        [ConstructorArgument("key")]
        public string Key { get; set; } = string.Empty;

        public override object ProvideValue(IServiceProvider serviceProvider) => Loc.Get(Key);
    }
}
