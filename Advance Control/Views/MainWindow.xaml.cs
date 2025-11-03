using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.Services.OnlineCheck;

namespace Advance_Control
{
    public sealed partial class MainWindow : Window
    {
        private readonly IOnlineCheck _onlineCheck;

        // Constructor adaptado para que DI inyecte IOnlineCheck.
        public MainWindow(IOnlineCheck onlineCheck)
        {
            this.InitializeComponent();
            _onlineCheck = onlineCheck ?? throw new ArgumentNullException(nameof(onlineCheck));
        }

        // Handler del botón que usa _onlineCheck sin bloquear el hilo de UI.
        private async void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            CheckButton.IsEnabled = false;
            StatusTextBlock.Text = "Comprobando...";

            try
            {
                var result = await _onlineCheck.CheckAsync();

                if (result.IsOnline)
                {
                    StatusTextBlock.Text = "API ONLINE";
                }
                else if (result.StatusCode.HasValue)
                {
                    StatusTextBlock.Text = $"API returned status {result.StatusCode}: {result.ErrorMessage}";
                }
                else
                {
                    StatusTextBlock.Text = $"Network error: {result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                // Captura cualquier excepción inesperada y mostrar un mensaje útil.
                StatusTextBlock.Text = $"Error inesperado: {ex.Message}";
            }
            finally
            {
                CheckButton.IsEnabled = true;
            }
        }
    }
}