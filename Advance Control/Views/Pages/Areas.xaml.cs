using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.ViewModels;
using Advance_Control.Services.Logging;
using Advance_Control.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.UI.Xaml.Navigation;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Página de gestión de áreas geográficas con Google Maps
    /// </summary>
    public sealed partial class Areas : Page
    {
        // Default coordinates for Mexico City if configuration is not available
        private const string DEFAULT_LATITUDE = "19.4326";
        private const string DEFAULT_LONGITUDE = "-99.1332";
        
        public AreasViewModel ViewModel { get; }
        private readonly ILoggingService _loggingService;
        private bool _isEditMode = false;
        private int? _editingAreaId = null;
        private bool _isFormVisible = false;
        
        // Store polygon/shape data from Google Maps Drawing Manager
        private string? _currentShapeType = null;
        private string? _currentShapePath = null;
        private string? _currentShapeCenter = null;
        private decimal? _currentShapeRadius = null;
        private string? _currentShapeBounds = null;

        // Reference to parent Ubicaciones page for map operations
        public Ubicaciones? ParentUbicacionesPage { get; set; }

        public Areas()
        {
            // Resolver el ViewModel desde DI
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<AreasViewModel>();

            // Resolver el servicio de logging desde DI
            _loggingService = ((App)Application.Current).Host.Services.GetRequiredService<ILoggingService>();

            this.InitializeComponent();

            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;

            // No need for WebView2 setup since map is managed by parent Ubicaciones page
            this.Loaded += Areas_Loaded;
        }

        private async void Areas_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize ViewModel when page loads
                await ViewModel.InitializeAsync();
                await _loggingService.LogInformationAsync("Página de Áreas cargada", "Areas", "Areas_Loaded");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al cargar página de Áreas", ex, "Areas", "Areas_Loaded");
            }
        }

        /// <summary>
        /// Maneja los mensajes de forma (shape) recibidos del mapa
        /// Llamado por la página padre Ubicaciones
        /// </summary>
        public async Task HandleShapeMessageAsync(Dictionary<string, JsonElement> jsonDoc)
        {
            try
            {
                if (jsonDoc.TryGetValue("type", out var typeElement))
                {
                    var messageType = typeElement.GetString();

                    if (messageType == "shapeDrawn" || messageType == "shapeEdited")
                    {
                        // Extract shape data
                        if (jsonDoc.TryGetValue("shapeType", out var shapeTypeElement))
                        {
                            _currentShapeType = shapeTypeElement.GetString();
                        }

                        if (jsonDoc.TryGetValue("path", out var pathElement))
                        {
                            _currentShapePath = pathElement.GetRawText();
                        }

                        if (jsonDoc.TryGetValue("center", out var centerElement))
                        {
                            _currentShapeCenter = centerElement.GetRawText();
                        }

                        if (jsonDoc.TryGetValue("radius", out var radiusElement) && radiusElement.ValueKind == JsonValueKind.Number)
                        {
                            _currentShapeRadius = radiusElement.GetDecimal();
                        }

                        if (jsonDoc.TryGetValue("bounds", out var boundsElement))
                        {
                            _currentShapeBounds = boundsElement.GetRawText();
                        }

                        await _loggingService.LogInformationAsync(
                            $"Shape {messageType}: Type={_currentShapeType}",
                            "Areas",
                            "HandleShapeMessageAsync");
                    }
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al procesar mensaje de forma", ex, "Areas", "HandleShapeMessageAsync");
            }
        }


        private object? ParsePathJson(AreaDto area)
        {
            // Si el área tiene MetadataJSON, intentar extraer el path de allí
            if (!string.IsNullOrEmpty(area.MetadataJSON))
            {
                try
                {
                    var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(area.MetadataJSON);
                    if (metadata != null && metadata.TryGetValue("path", out var pathElement))
                    {
                        return JsonSerializer.Deserialize<object>(pathElement.GetRawText());
                    }
                }
                catch
                {
                    // Ignore parsing errors
                }
            }
            return null;
        }

        private object? ParseCenterJson(AreaDto area)
        {
            if (area.CentroLatitud.HasValue && area.CentroLongitud.HasValue)
            {
                return new { lat = area.CentroLatitud.Value, lng = area.CentroLongitud.Value };
            }
            return null;
        }

       

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Refresh areas data
                await ViewModel.RefreshAreasAsync();
                
                // Reload the map through parent Ubicaciones page
                if (ParentUbicacionesPage != null)
                {
                    await ParentUbicacionesPage.ReloadAreasMapAsync();
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al refrescar áreas", ex, "Areas", "RefreshButton_Click");
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            _isEditMode = false;
            _editingAreaId = null;
            FormTitle.Text = "Nueva Área";
            
            // Clear form
            NombreTextBox.Text = string.Empty;
            DescripcionTextBox.Text = string.Empty;
            ColorComboBox.SelectedIndex = 0;
            ActivoCheckBox.IsChecked = true;

            // Show form
            AreaForm.Visibility = Visibility.Visible;
            _isFormVisible = true;
        }

        private void AreasList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle area selection - could zoom to area on map
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int areaId)
            {
                var area = ViewModel.Areas.FirstOrDefault(a => a.IdArea == areaId);
                if (area != null)
                {
                    _isEditMode = true;
                    _editingAreaId = areaId;
                    FormTitle.Text = "Editar Área";

                    // Populate form
                    NombreTextBox.Text = area.Nombre;
                    DescripcionTextBox.Text = area.Descripcion;
                    ActivoCheckBox.IsChecked = area.Activo ?? true;

                    // Set color
                    for (int i = 0; i < ColorComboBox.Items.Count; i++)
                    {
                        if (ColorComboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == area.ColorMapa)
                        {
                            ColorComboBox.SelectedIndex = i;
                            break;
                        }
                    }

                    // Show form
                    AreaForm.Visibility = Visibility.Visible;
                    _isFormVisible = true;

                    // TODO: Load area shape on map for editing
                }
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int areaId)
            {
                var area = ViewModel.Areas.FirstOrDefault(a => a.IdArea == areaId);
                if (area != null)
                {
                    // Show confirmation dialog
                    var dialog = new ContentDialog
                    {
                        Title = "Confirmar eliminación",
                        Content = $"¿Está seguro de que desea eliminar el área '{area.Nombre}'?",
                        PrimaryButtonText = "Eliminar",
                        CloseButtonText = "Cancelar",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = this.XamlRoot
                    };

                    var result = await dialog.ShowAsync();

                    if (result == ContentDialogResult.Primary)
                    {
                        var deleteResult = await ViewModel.DeleteAreaAsync(areaId);

                        if (deleteResult.Success)
                        {
                            // Show success message
                            var successDialog = new ContentDialog
                            {
                                Title = "Éxito",
                                Content = "Área eliminada correctamente.",
                                CloseButtonText = "OK",
                                XamlRoot = this.XamlRoot
                            };
                            await successDialog.ShowAsync();

                            // Reload map through parent Ubicaciones page
                            if (ParentUbicacionesPage != null)
                            {
                                await ParentUbicacionesPage.ReloadAreasMapAsync();
                            }
                        }
                        else
                        {
                            // Show error message
                            var errorDialog = new ContentDialog
                            {
                                Title = "Error",
                                Content = deleteResult.Message ?? "Error al eliminar el área.",
                                CloseButtonText = "OK",
                                XamlRoot = this.XamlRoot
                            };
                            await errorDialog.ShowAsync();
                        }
                    }
                }
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate form
            if (string.IsNullOrWhiteSpace(NombreTextBox.Text))
            {
                var dialog = new ContentDialog
                {
                    Title = "Validación",
                    Content = "El nombre del área es requerido.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }

            // When editing, allow saving without redrawing the shape
            // When creating, require a shape to be drawn
            // For circles, we need center and radius; for polygons/rectangles, we need path
            if (!_isEditMode && string.IsNullOrEmpty(_currentShapeType))
            {
                var dialog = new ContentDialog
                {
                    Title = "Validación",
                    Content = "Debe dibujar un área en el mapa antes de guardar.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }

            // Validate that we have the necessary data for the specific shape type
            if (!_isEditMode)
            {
                bool hasValidShapeData = false;
                
                if (_currentShapeType?.ToLower() == "circle")
                {
                    // For circles, we need center and radius
                    hasValidShapeData = !string.IsNullOrEmpty(_currentShapeCenter) && _currentShapeRadius.HasValue;
                }
                else
                {
                    // For polygons and rectangles, we need path
                    hasValidShapeData = !string.IsNullOrEmpty(_currentShapePath);
                }

                if (!hasValidShapeData)
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Validación",
                        Content = "Debe dibujar un área en el mapa antes de guardar.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                    return;
                }
            }

            // Get selected color
            string selectedColor = "#FF0000";
            if (ColorComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                selectedColor = selectedItem.Tag.ToString() ?? "#FF0000";
            }

            // Create or update area
            var area = new AreaDto
            {
                IdArea = _editingAreaId ?? 0,
                Nombre = NombreTextBox.Text.Trim(),
                Descripcion = DescripcionTextBox.Text?.Trim(),
                ColorMapa = selectedColor,
                Opacidad = 0.35m,
                ColorBorde = selectedColor,
                AnchoBorde = 2,
                Activo = ActivoCheckBox.IsChecked,
                TipoGeometria = _currentShapeType ?? "Polygon",
                // Store complete shape data as JSON for later retrieval
                // This allows us to reconstruct the exact shape when editing
                MetadataJSON = _isEditMode && string.IsNullOrEmpty(_currentShapePath)
                    ? null  // Keep existing metadata when editing without redrawing
                    : JsonSerializer.Serialize(new Dictionary<string, object?>
                    {
                        ["path"] = _currentShapePath != null ? JsonSerializer.Deserialize<JsonElement>(_currentShapePath) : (object?)null,
                        ["center"] = _currentShapeCenter != null ? JsonSerializer.Deserialize<JsonElement>(_currentShapeCenter) : (object?)null,
                        ["bounds"] = _currentShapeBounds != null ? JsonSerializer.Deserialize<JsonElement>(_currentShapeBounds) : (object?)null,
                        ["radius"] = _currentShapeRadius
                    })
            };

            // Parse center from JSON
            if (!string.IsNullOrEmpty(_currentShapeCenter))
            {
                try
                {
                    var center = JsonSerializer.Deserialize<Dictionary<string, decimal>>(_currentShapeCenter);
                    if (center != null)
                    {
                        area.CentroLatitud = center.GetValueOrDefault("lat");
                        area.CentroLongitud = center.GetValueOrDefault("lng");
                    }
                }
                catch { }
            }

            // Parse bounds from JSON
            if (!string.IsNullOrEmpty(_currentShapeBounds))
            {
                try
                {
                    var bounds = JsonSerializer.Deserialize<Dictionary<string, decimal>>(_currentShapeBounds);
                    if (bounds != null)
                    {
                        area.BoundingBoxNE_Lat = bounds.GetValueOrDefault("north");
                        area.BoundingBoxNE_Lng = bounds.GetValueOrDefault("east");
                        area.BoundingBoxSW_Lat = bounds.GetValueOrDefault("south");
                        area.BoundingBoxSW_Lng = bounds.GetValueOrDefault("west");
                    }
                }
                catch { }
            }

            // Set radius for circles
            if (_currentShapeRadius.HasValue)
            {
                area.Radio = _currentShapeRadius.Value;
            }

            ApiResponse result;
            if (_isEditMode && _editingAreaId.HasValue)
            {
                result = await ViewModel.UpdateAreaAsync(area);
            }
            else
            {
                result = await ViewModel.CreateAreaAsync(area);
            }

            if (result.Success)
            {
                // Show success message
                var successDialog = new ContentDialog
                {
                    Title = "Éxito",
                    Content = _isEditMode ? "Área actualizada correctamente." : "Área creada correctamente.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await successDialog.ShowAsync();

                // Hide form and reset
                CancelButton_Click(sender, e);

                // Clear current shape and reload map through parent page
                if (ParentUbicacionesPage != null)
                {
                    await ParentUbicacionesPage.ExecuteMapScriptAsync("clearCurrentShape();");
                    await ParentUbicacionesPage.ReloadAreasMapAsync();
                }
            }
            else
            {
                // Show error message
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = result.Message ?? "Error al guardar el área.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide form
            AreaForm.Visibility = Visibility.Collapsed;
            _isFormVisible = false;

            // Reset state
            _isEditMode = false;
            _editingAreaId = null;
            _currentShapeType = null;
            _currentShapePath = null;
            _currentShapeCenter = null;
            _currentShapeRadius = null;
            _currentShapeBounds = null;

            // Clear selection
            AreasList.SelectedItem = null;
        }
    }
}
