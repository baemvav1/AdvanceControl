using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Advance_Control.Models;
using Advance_Control.Services.Refacciones;
using Microsoft.Extensions.DependencyInjection;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// UserControl para visualizar los detalles de una refacción
    /// </summary>
    public sealed partial class RefaccionesViewerUserControl : UserControl
    {
        private readonly IRefaccionService? _refaccionService;
        private RefaccionDto? _refaccion;

        /// <summary>
        /// Constructor que acepta una refacción ya cargada
        /// </summary>
        public RefaccionesViewerUserControl(RefaccionDto refaccion)
        {
            this.InitializeComponent();
            
            _refaccion = refaccion;
            
            // Cargar datos de la refacción
            this.Loaded += OnLoaded;
        }

        /// <summary>
        /// Constructor que acepta el ID de una refacción para cargarla desde el servicio
        /// </summary>
        public RefaccionesViewerUserControl(int idRefaccion)
        {
            this.InitializeComponent();
            
            // Resolve service from DI
            _refaccionService = ((App)Application.Current).Host.Services.GetRequiredService<IRefaccionService>();
            
            // Cargar datos de la refacción
            this.Loaded += async (s, e) => await LoadRefaccionAsync(idRefaccion);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_refaccion != null)
            {
                DisplayRefaccion(_refaccion);
                ContentPanel.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Carga una refacción específica por su ID
        /// </summary>
        private async Task LoadRefaccionAsync(int idRefaccion)
        {
            if (_refaccionService == null)
            {
                ErrorTextBlock.Text = "Error: Servicio no disponible";
                ErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                LoadingRing.Visibility = Visibility.Visible;
                LoadingRing.IsActive = true;
                ContentPanel.Visibility = Visibility.Collapsed;
                ErrorTextBlock.Visibility = Visibility.Collapsed;

               

                var query = new RefaccionQueryDto
                {
                    IdRefaccion = idRefaccion
                };

                var refacciones = await _refaccionService.GetRefaccionesAsync(query, CancellationToken.None);

                _refaccion = refacciones.Count > 0 ? refacciones[0] : null;

                if (_refaccion != null)
                {
                    DisplayRefaccion(_refaccion);
                    ContentPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    ErrorTextBlock.Text = "No se encontró la refacción solicitada";
                    ErrorTextBlock.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar refacción: {ex.GetType().Name} - {ex.Message}");
                ErrorTextBlock.Text = "Error al cargar la refacción. Intente nuevamente.";
                ErrorTextBlock.Visibility = Visibility.Visible;
            }
            finally
            {
                LoadingRing.Visibility = Visibility.Collapsed;
                LoadingRing.IsActive = false;
            }
        }

        /// <summary>
        /// Muestra los datos de la refacción en los controles de la UI
        /// </summary>
        private void DisplayRefaccion(RefaccionDto refaccion)
        {
            IdTextRun.Text = refaccion.IdRefaccion.ToString();
            MarcaTextBlock.Text = refaccion.Marca ?? "Sin marca";
            SerieTextBlock.Text = refaccion.Serie ?? "Sin serie";
            CostoTextBlock.Text = refaccion.Costo.HasValue ? $"${refaccion.Costo.Value:F2}" : "Sin costo";
            DescripcionTextBlock.Text = !string.IsNullOrWhiteSpace(refaccion.Descripcion) 
                ? refaccion.Descripcion 
                : "Sin descripción";

            // Configurar el estatus con colores apropiados
            if (refaccion.Estatus.HasValue && refaccion.Estatus.Value)
            {
                EstatusTextBlock.Text = "Activo";
                EstatusBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.Green) { Opacity = 0.2 };
                EstatusTextBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);
            }
            else
            {
                EstatusTextBlock.Text = "Inactivo";
                EstatusBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.Red) { Opacity = 0.2 };
                EstatusTextBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
            }
        }
    }
}
