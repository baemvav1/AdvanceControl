using System;
using Advance_Control.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Items.Administracion
{
    public sealed partial class PermisoAccionItemView : UserControl
    {
        public static readonly DependencyProperty PermisoAccionProperty =
            DependencyProperty.Register(nameof(PermisoAccion), typeof(PermisoAccionModuloDto), typeof(PermisoAccionItemView), new PropertyMetadata(null));

        public event EventHandler<PermisoAccionNivelEditRequestedEventArgs>? NivelEditRequested;

        public PermisoAccionItemView()
        {
            InitializeComponent();
        }

        public PermisoAccionModuloDto? PermisoAccion
        {
            get => (PermisoAccionModuloDto?)GetValue(PermisoAccionProperty);
            set => SetValue(PermisoAccionProperty, value);
        }

        public string BuildMetadata(PermisoAccionModuloDto? permisoAccion)
        {
            if (permisoAccion == null)
                return string.Empty;

            return $"Tipo: {permisoAccion.TipoAccion}  |  Control: {permisoAccion.ControlKey ?? "Sin clave"}";
        }

        public string BuildNivelText(PermisoAccionModuloDto? permisoAccion)
        {
            if (permisoAccion == null)
                return string.Empty;

            return $"Nivel requerido: {permisoAccion.NivelRequerido}";
        }

        private void CambiarNivelButton_Click(object sender, RoutedEventArgs e)
        {
            if (PermisoAccion == null)
                return;

            NivelEditRequested?.Invoke(this, new PermisoAccionNivelEditRequestedEventArgs
            {
                IdPermisoAccionModulo = PermisoAccion.IdPermisoAccionModulo
            });
        }
    }
}
