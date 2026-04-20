using Advance_Control.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Advance_Control.Views
{
    public sealed partial class UsuarioInfoView : UserControl
    {
        private static readonly SolidColorBrush _greenBrush = new(global::Windows.UI.Color.FromArgb(255, 16, 185, 129));
        private static readonly SolidColorBrush _grayBrush = new(global::Windows.UI.Color.FromArgb(255, 156, 163, 175));

        public UsuarioInfoView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Actualiza la vista con la información del usuario seleccionado.
        /// Pasar null para mostrar el estado vacío.
        /// </summary>
        public void SetUsuario(UsuarioChatDto? usuario)
        {
            if (usuario == null)
            {
                EmptyState.Visibility = Visibility.Visible;
                InfoPanel.Visibility = Visibility.Collapsed;
                return;
            }

            EmptyState.Visibility = Visibility.Collapsed;
            InfoPanel.Visibility = Visibility.Visible;

            AvatarIniciales.Text = usuario.Iniciales;
            NombreText.Text = usuario.NombreVisible ?? "";
            UsuarioText.Text = $"@{usuario.Usuario}";

            EstadoIndicator.Fill = usuario.EstaEnLinea ? _greenBrush : _grayBrush;
            EstadoText.Text = usuario.EstaEnLinea ? "En línea" : "Desconectado";

            NivelText.Text = usuario.Nivel.HasValue ? $"Nivel {usuario.Nivel}" : "Sin asignar";
            CuentaEstadoText.Text = usuario.EstaActiva ? "Activa" : "Inactiva";
        }
    }
}
