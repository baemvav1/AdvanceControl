using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Advance_Control.Models;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Equipos
{
    public sealed partial class SeleccionarUbicacionUserControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ObservableCollection<UbicacionDto> _ubicaciones = new ObservableCollection<UbicacionDto>();
        private UbicacionDto? _selectedUbicacion;
        private bool _isLoading = false;

        public SeleccionarUbicacionUserControl()
        {
            this.InitializeComponent();
        }

        public ObservableCollection<UbicacionDto> Ubicaciones
        {
            get => _ubicaciones;
            set
            {
                if (_ubicaciones != value)
                {
                    _ubicaciones = value;
                    OnPropertyChanged();
                }
            }
        }

        public UbicacionDto? SelectedUbicacion
        {
            get => _selectedUbicacion;
            set
            {
                if (_selectedUbicacion != value)
                {
                    _selectedUbicacion = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
