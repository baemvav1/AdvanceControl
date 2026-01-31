using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Advance_Control.Models;
using Advance_Control.Services.Refacciones;
using Microsoft.Extensions.DependencyInjection;

namespace Advance_Control.UserControls
{
    /// <summary>
    /// UserControl para visualizar los detalles completos de una refacción
    /// </summary>
    public sealed partial class RefaccionesViewerUserControl : UserControl, INotifyPropertyChanged
    {
        private readonly IRefaccionService _refaccionService;
        private int _idRefaccion;
        private string? _marca;
        private string? _serie;
        private double? _costo;
        private string? _descripcion;
        private bool? _estatus;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Constructor sin parámetros requerido por el DialogService
        /// </summary>
        public RefaccionesViewerUserControl()
        {
            this.InitializeComponent();
            _refaccionService = ((App)Application.Current).Host.Services.GetRequiredService<IRefaccionService>();
            
            this.Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Si hay un RefaccionDto configurado, cargarlo
            if (RefaccionToDisplay != null)
            {
                LoadRefaccionData(RefaccionToDisplay);
            }
            // Si hay un IdRefaccion configurado, cargarlo desde el servicio
            else if (IdRefaccionToLoad > 0)
            {
                await LoadRefaccionByIdAsync(IdRefaccionToLoad);
            }
        }

        /// <summary>
        /// RefaccionDto para mostrar (se configura antes de mostrar el control)
        /// </summary>
        public RefaccionDto? RefaccionToDisplay { get; set; }

        /// <summary>
        /// ID de refacción para cargar desde el servicio (se configura antes de mostrar el control)
        /// </summary>
        public int IdRefaccionToLoad { get; set; }

        public int IdRefaccion
        {
            get => _idRefaccion;
            private set
            {
                if (_idRefaccion != value)
                {
                    _idRefaccion = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? Marca
        {
            get => _marca;
            private set
            {
                if (_marca != value)
                {
                    _marca = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? Serie
        {
            get => _serie;
            private set
            {
                if (_serie != value)
                {
                    _serie = value;
                    OnPropertyChanged();
                }
            }
        }

        public double? Costo
        {
            get => _costo;
            private set
            {
                if (_costo != value)
                {
                    _costo = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? Descripcion
        {
            get => _descripcion;
            private set
            {
                if (_descripcion != value)
                {
                    _descripcion = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool? Estatus
        {
            get => _estatus;
            private set
            {
                if (_estatus != value)
                {
                    _estatus = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(EstatusText));
                    OnPropertyChanged(nameof(EstatusBrush));
                }
            }
        }

        public string EstatusText => Estatus == true ? "Activo" : "Inactivo";

        public SolidColorBrush EstatusBrush => Estatus == true 
            ? new SolidColorBrush(Colors.Green) 
            : new SolidColorBrush(Colors.Red);

        /// <summary>
        /// Carga los datos de una refacción desde un RefaccionDto
        /// </summary>
        private void LoadRefaccionData(RefaccionDto refaccion)
        {
            IdRefaccion = refaccion.IdRefaccion;
            Marca = refaccion.Marca ?? "N/A";
            Serie = refaccion.Serie ?? "N/A";
            Costo = refaccion.Costo ?? 0;
            Descripcion = refaccion.Descripcion ?? "Sin descripción";
            Estatus = refaccion.Estatus ?? false;

            ShowContent();
        }

        /// <summary>
        /// Carga una refacción por su ID desde el servicio
        /// </summary>
        private async Task LoadRefaccionByIdAsync(int idRefaccion)
        {
            try
            {
                ShowLoading();

                // Buscar la refacción por ID
                var refacciones = await _refaccionService.GetRefaccionesAsync(null, CancellationToken.None);
                var refaccion = refacciones.FirstOrDefault(r => r.IdRefaccion == idRefaccion);

                if (refaccion != null)
                {
                    LoadRefaccionData(refaccion);
                }
                else
                {
                    ShowError($"No se encontró la refacción con ID {idRefaccion}");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error al cargar la refacción: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error en LoadRefaccionByIdAsync: {ex.GetType().Name} - {ex.Message}");
            }
        }

        private void ShowLoading()
        {
            LoadingRing.Visibility = Visibility.Visible;
            LoadingRing.IsActive = true;
            ContentScroller.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowContent()
        {
            LoadingRing.Visibility = Visibility.Collapsed;
            LoadingRing.IsActive = false;
            ContentScroller.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowError(string message)
        {
            LoadingRing.Visibility = Visibility.Collapsed;
            LoadingRing.IsActive = false;
            ContentScroller.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Visible;
            ErrorTextBlock.Text = message;
        }
    }
}
