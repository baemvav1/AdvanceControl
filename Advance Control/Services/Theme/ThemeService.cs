using System;
using Microsoft.UI.Xaml;
using Windows.Storage;

namespace Advance_Control.Services.Theme
{
    /// <summary>
    /// Implementación que persiste la preferencia de tema en LocalSettings
    /// y aplica RequestedTheme al FrameworkElement raíz.
    /// </summary>
    public sealed class ThemeService : IThemeService
    {
        private const string SettingsKey = "AppTheme";
        private FrameworkElement? _rootElement;

        public ElementTheme CurrentTheme { get; private set; } = ElementTheme.Default;

        public void Initialize(FrameworkElement rootElement)
        {
            _rootElement = rootElement ?? throw new ArgumentNullException(nameof(rootElement));

            var saved = ReadSavedTheme();
            ApplyTheme(saved);
        }

        public void SetTheme(ElementTheme theme)
        {
            ApplyTheme(theme);
            SaveTheme(theme);
        }

        private void ApplyTheme(ElementTheme theme)
        {
            CurrentTheme = theme;

            if (_rootElement != null)
                _rootElement.RequestedTheme = theme;
        }

        private static ElementTheme ReadSavedTheme()
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings.Values.TryGetValue(SettingsKey, out var value) && value is string s)
                {
                    return s switch
                    {
                        "light" => ElementTheme.Light,
                        "dark" => ElementTheme.Dark,
                        _ => ElementTheme.Default
                    };
                }
            }
            catch
            {
                // Si LocalSettings no está disponible, usar tema del sistema
            }

            return ElementTheme.Default;
        }

        private static void SaveTheme(ElementTheme theme)
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values[SettingsKey] = theme switch
                {
                    ElementTheme.Light => "light",
                    ElementTheme.Dark => "dark",
                    _ => "system"
                };
            }
            catch
            {
                // Silenciar si LocalSettings no está disponible
            }
        }
    }
}
