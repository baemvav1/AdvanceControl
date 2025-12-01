using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Advance_Control.Views.Equipos
{
    /// <summary>
    /// Vista para editar la nota de una relación equipo-cliente.
    /// </summary>
    public sealed partial class EditNotaRelacionView : UserControl, INotifyPropertyChanged
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
        /// Indica si se guardó exitosamente
        /// </summary>
        public bool SaveSuccessful { get; private set; }

        private string _nota = string.Empty;
        /// <summary>
        /// Nota de la relación
        /// </summary>
        public string Nota
        {
            get => _nota;
            set
            {
                if (_nota != value)
                {
                    _nota = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _clienteNombre = string.Empty;
        /// <summary>
        /// Nombre del cliente (para mostrar en el título)
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
        public EditNotaRelacionView()
        {
            this.InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSuccessful = true;
            CloseDialogAction?.Invoke();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSuccessful = false;
            CloseDialogAction?.Invoke();
        }
    }
}
