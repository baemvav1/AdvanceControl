using Advance_Control.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Linq;

namespace Advance_Control.Views.Items.Administracion
{
    public sealed partial class UsuarioAdminItemView : UserControl
    {
        private static readonly SolidColorBrush InactiveBackgroundBrush = new(Colors.Black);

        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register(
                nameof(IsExpanded),
                typeof(bool),
                typeof(UsuarioAdminItemView),
                new PropertyMetadata(false));

        public static readonly DependencyProperty UsuarioProperty =
            DependencyProperty.Register(
                nameof(Usuario),
                typeof(UsuarioAdminDto),
                typeof(UsuarioAdminItemView),
                new PropertyMetadata(null));

        public UsuarioAdminDto? Usuario
        {
            get => (UsuarioAdminDto?)GetValue(UsuarioProperty);
            set => SetValue(UsuarioProperty, value);
        }

        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        public event EventHandler<UsuarioAdminActionEventArgs>? EditRequested;
        public event EventHandler<UsuarioAdminActionEventArgs>? DeleteRequested;

        public UsuarioAdminItemView()
        {
            InitializeComponent();
        }

        public string FormatOptionalText(string? value, string fallback)
            => string.IsNullOrWhiteSpace(value) ? fallback : value;

        public string FormatNullableNumber(long? value, string fallback)
            => value?.ToString() ?? fallback;

        public string FormatNullableNumber(int? value, string fallback)
            => value?.ToString() ?? fallback;

        public string FormatEstado(bool estaActiva)
            => estaActiva ? "Activo" : "Inactivo";

        public Brush ResolveBackground(bool estaActiva)
        {
            if (!estaActiva)
            {
                return InactiveBackgroundBrush;
            }

            return (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
        }

        public string FormatNombre(string? tratamiento, string? nombre, string? apellido, long? contactoId)
        {
            var nombreCompleto = string.Join(" ", new[] { tratamiento, nombre, apellido }.Where(value => !string.IsNullOrWhiteSpace(value)));
            if (!string.IsNullOrWhiteSpace(nombreCompleto))
                return nombreCompleto;

            return contactoId.HasValue ? "Contacto sin nombre capturado" : "Sin contacto asignado";
        }

        private void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            if (Usuario is not null)
            {
                EditRequested?.Invoke(this, new UsuarioAdminActionEventArgs(Usuario));
            }
        }

        private void EliminarButton_Click(object sender, RoutedEventArgs e)
        {
            if (Usuario is not null)
            {
                DeleteRequested?.Invoke(this, new UsuarioAdminActionEventArgs(Usuario));
            }
        }
    }

    public sealed class UsuarioAdminActionEventArgs : EventArgs
    {
        public UsuarioAdminActionEventArgs(UsuarioAdminDto usuario)
        {
            Usuario = usuario ?? throw new ArgumentNullException(nameof(usuario));
        }

        public UsuarioAdminDto Usuario { get; }
    }
}
