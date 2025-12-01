using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Advance_Control.Views.Equipos
{
    /// <summary>
    /// Vista para confirmar la eliminación de una relación equipo-cliente.
    /// </summary>
    public sealed partial class ConfirmDeleteRelacionView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Acción para cerrar el diálogo
        /// </summary>
        public Action? CloseDialogAction { get; set; }

        /// <summary>
        /// Indica si se confirmó la eliminación
        /// </summary>
        public bool DeleteConfirmed { get; private set; }

        private string _clienteNombre = string.Empty;
        /// <summary>
        /// Nombre del cliente (para mostrar en el mensaje de confirmación)
        /// </summary>
        public string ClienteNombre
        {
            get => _clienteNombre;
            set
            {
                if (_clienteNombre != value)
                {
                    _clienteNombre = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ConfirmDeleteRelacionView()
        {
            this.InitializeComponent();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteConfirmed = true;
            CloseDialogAction?.Invoke();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteConfirmed = false;
            CloseDialogAction?.Invoke();
        }
    }
}
