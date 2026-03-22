using System;
using Advance_Control.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Items.Administracion
{
    public sealed partial class PermisoModuloItemView : UserControl
    {
        public static readonly DependencyProperty PermisoProperty =
            DependencyProperty.Register(nameof(Permiso), typeof(PermisoModuloDto), typeof(PermisoModuloItemView), new PropertyMetadata(null));

        public event EventHandler<PermisoModuloNivelEditRequestedEventArgs>? NivelModuloEditRequested;
        public event EventHandler<PermisoAccionNivelEditRequestedEventArgs>? NivelAccionEditRequested;

        public PermisoModuloItemView()
        {
            InitializeComponent();
        }

        public PermisoModuloDto? Permiso
        {
            get => (PermisoModuloDto?)GetValue(PermisoProperty);
            set => SetValue(PermisoProperty, value);
        }

        public string BuildModuleMetadata(PermisoModuloDto? permiso)
        {
            if (permiso == null)
                return string.Empty;

            return $"{permiso.GrupoModulo}  |  {permiso.NombreView}  |  {permiso.RutaView}";
        }

        public string BuildNivelText(PermisoModuloDto? permiso)
        {
            if (permiso == null)
                return string.Empty;

            return $"Nivel requerido: {permiso.NivelRequerido}";
        }

        private void CambiarNivelButton_Click(object sender, RoutedEventArgs e)
        {
            if (Permiso == null)
                return;

            NivelModuloEditRequested?.Invoke(this, new PermisoModuloNivelEditRequestedEventArgs
            {
                IdPermisoModulo = Permiso.IdPermisoModulo
            });
        }

        private void PermisoAccionItemView_NivelEditRequested(object sender, PermisoAccionNivelEditRequestedEventArgs e)
        {
            NivelAccionEditRequested?.Invoke(this, e);
        }
    }
}
