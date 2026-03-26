using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Advance_Control.Services.Theme;
using Advance_Control.Utilities;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Página de configuración general de la aplicación.
    /// Contiene el selector de tema (Sistema / Claro / Oscuro).
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private readonly IThemeService _themeService;
        private bool _isInitializing;

        public SettingsPage()
        {
            _themeService = AppServices.Get<IThemeService>();
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Sincronizar el ComboBox con el tema actual sin disparar el evento
            _isInitializing = true;
            ThemeComboBox.SelectedIndex = _themeService.CurrentTheme switch
            {
                ElementTheme.Light => 1,
                ElementTheme.Dark => 2,
                _ => 0 // Default = Sistema
            };
            _isInitializing = false;
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing)
                return;

            if (ThemeComboBox.SelectedItem is ComboBoxItem selected && selected.Tag is string tag)
            {
                var theme = tag switch
                {
                    "Light" => ElementTheme.Light,
                    "Dark" => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };

                _themeService.SetTheme(theme);
            }
        }
    }
}
