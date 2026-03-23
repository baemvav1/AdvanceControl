using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Equipos;
using Advance_Control.Services.Levantamiento;
using Advance_Control.Services.LocalStorage;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Reportes;
using Advance_Control.Services.Session;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Advance_Control.ViewModels
{
    public class LevantamientoViewModel : ViewModelBase
    {
        private const double BaseImageWidth = 1152d;
        private const double BaseImageHeight = 928d;

        private readonly ILoggingService _logger;
        private readonly IEquipoService _equipoService;
        private readonly ILevantamientoApiService _levantamientoApiService;
        private readonly ILevantamientoReportService _reportService;
        private readonly ILevantamientoImageService _imageService;
        private readonly Lazy<IUserSessionService> _session;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        private readonly Dictionary<string, string> _tagToHotspotKey = new Dictionary<string, string>(StringComparer.Ordinal);
        private bool _isLoading;
        private string? _errorMessage;
        private bool _isInitialized;
        private LevantamientoHotspotItem? _selectedHotspot;
        private LevantamientoReporteDto _reporte = new LevantamientoReporteDto();
        private List<EquipoDto> _allEquipos = new();
        private EquipoDto? _equipoSeleccionado;
        private bool _isLoadingEquipos;
        private int _idLevantamiento;
        private bool _isSaving;
        private string? _introduccionCargada;
        private string? _conclusionCargada;

        public LevantamientoViewModel(
            ILoggingService logger,
            IEquipoService equipoService,
            ILevantamientoApiService levantamientoApiService,
            ILevantamientoReportService reportService,
            ILevantamientoImageService imageService,
            Lazy<IUserSessionService> session)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _equipoService = equipoService ?? throw new ArgumentNullException(nameof(equipoService));
            _levantamientoApiService = levantamientoApiService ?? throw new ArgumentNullException(nameof(levantamientoApiService));
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            Hotspots = new ObservableCollection<LevantamientoHotspotItem>();
            HotspotsConFalla = new ObservableCollection<LevantamientoHotspotItem>();
            CapturedTreeItems = new ObservableCollection<LevantamientoTreeItemModel>();
            EquiposSugeridos = new ObservableCollection<string>();
            HotspotImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Images/ImagenHotSpotElevadorDeTraccion.jpg"));
            BuildBaseState();
        }

        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        public string? ErrorMessage
        {
            get { return _errorMessage; }
            set { SetProperty(ref _errorMessage, value); }
        }

        public ObservableCollection<LevantamientoHotspotItem> Hotspots { get; }

        public ObservableCollection<LevantamientoHotspotItem> HotspotsConFalla { get; }

        public ObservableCollection<LevantamientoTreeItemModel> CapturedTreeItems { get; }

        public ObservableCollection<string> EquiposSugeridos { get; }

        public EquipoDto? EquipoSeleccionado
        {
            get { return _equipoSeleccionado; }
            private set
            {
                if (SetProperty(ref _equipoSeleccionado, value))
                {
                    OnPropertyChanged(nameof(TieneEquipoSeleccionado));
                    OnPropertyChanged(nameof(EquipoSeleccionadoIdentificador));
                    OnPropertyChanged(nameof(EquipoSeleccionadoMarca));
                }
            }
        }

        public bool TieneEquipoSeleccionado => EquipoSeleccionado is not null;

        public string EquipoSeleccionadoIdentificador => EquipoSeleccionado?.Identificador ?? string.Empty;

        public string EquipoSeleccionadoMarca => EquipoSeleccionado?.Marca ?? string.Empty;

        public int IdLevantamiento
        {
            get { return _idLevantamiento; }
            private set { SetProperty(ref _idLevantamiento, value); }
        }

        public bool IsSaving
        {
            get { return _isSaving; }
            private set { SetProperty(ref _isSaving, value); }
        }

        public bool TieneLevantamientoGuardado => IdLevantamiento > 0;

        public string? IntroduccionCargada
        {
            get { return _introduccionCargada; }
            private set { SetProperty(ref _introduccionCargada, value); }
        }

        public string? ConclusionCargada
        {
            get { return _conclusionCargada; }
            private set { SetProperty(ref _conclusionCargada, value); }
        }

        public bool IsLoadingEquipos
        {
            get { return _isLoadingEquipos; }
            private set { SetProperty(ref _isLoadingEquipos, value); }
        }

        public BitmapImage HotspotImageSource { get; }

        public double ImageWidth => BaseImageWidth;

        public double ImageHeight => BaseImageHeight;

        public LevantamientoHotspotItem? SelectedHotspot
        {
            get { return _selectedHotspot; }
            private set { SetProperty(ref _selectedHotspot, value); }
        }

        public string JsonLevantamiento => JsonSerializer.Serialize(_reporte, _jsonOptions);

        public async Task InitializeAsync()
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;
            ErrorMessage = null;

            try
            {
                if (!_isInitialized)
                {
                    BuildBaseState();
                }

                await CargarEquiposAsync();
                await _logger.LogInformationAsync("Levantamiento inicializado", nameof(LevantamientoViewModel), nameof(InitializeAsync));
            }
            catch (Exception ex)
            {
                ErrorMessage = "No se pudo inicializar el modulo de levantamiento.";
                await _logger.LogErrorAsync("Error al inicializar levantamiento", ex, nameof(LevantamientoViewModel), nameof(InitializeAsync));
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Carga un levantamiento existente desde el servidor por su ID,
        /// restaurando el equipo, introduccion, conclusion y los nodos con falla.
        /// </summary>
        public async Task CargarLevantamientoAsync(int idLevantamiento, CancellationToken cancellationToken = default)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var detail = await _levantamientoApiService.ObtenerLevantamientoAsync(idLevantamiento, cancellationToken);
                if (detail is null)
                {
                    ErrorMessage = $"No se encontro el levantamiento #{idLevantamiento}.";
                    return;
                }

                // Asignar ID para que las actualizaciones usen PUT en vez de POST
                IdLevantamiento = detail.IdLevantamiento;
                OnPropertyChanged(nameof(TieneLevantamientoGuardado));

                // Cargar introduccion y conclusion
                IntroduccionCargada = detail.Introduccion;
                ConclusionCargada = detail.Conclusion;

                // Seleccionar el equipo correspondiente
                var equipo = _allEquipos.FirstOrDefault(e => e.IdEquipo == detail.IdEquipo);
                EquipoSeleccionado = equipo;

                // Restaurar nodos con falla en los hotspots y en _reporte
                await RestaurarNodosDesdeServidorAsync(detail.Nodos);

                await _logger.LogInformationAsync(
                    $"Levantamiento #{idLevantamiento} cargado exitosamente",
                    nameof(LevantamientoViewModel), nameof(CargarLevantamientoAsync));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar levantamiento: {ex.Message}";
                await _logger.LogErrorAsync(
                    $"Error al cargar levantamiento {idLevantamiento}", ex,
                    nameof(LevantamientoViewModel), nameof(CargarLevantamientoAsync));
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Recorre los nodos del servidor, aplica las fallas a los hotspots/reporte
        /// y carga las imagenes existentes en disco para cada nodo.
        /// </summary>
        private async Task RestaurarNodosDesdeServidorAsync(IReadOnlyList<LevantamientoNodoDetailResponse> nodos)
        {
            foreach (var nodo in nodos)
            {
                if (nodo.TieneFalla)
                {
                    var hotspot = TryGetHotspotByKey(nodo.Clave);
                    if (hotspot is not null)
                    {
                        hotspot.DescripcionFalla = nodo.DescripcionFalla;

                        // Buscar imagenes existentes en disco para este nodo
                        try
                        {
                            var imagenesEnDisco = await _imageService.GetImagesAsync(IdLevantamiento, nodo.Clave);
                            foreach (var img in imagenesEnDisco)
                            {
                                hotspot.AddImage(img.FilePath);
                            }
                        }
                        catch (Exception ex)
                        {
                            await _logger.LogErrorAsync(
                                $"Error al cargar imagenes del nodo {nodo.Clave}", ex,
                                nameof(LevantamientoViewModel), nameof(RestaurarNodosDesdeServidorAsync));
                        }

                        UpdateReportNode(hotspot);
                    }
                }

                // Recorrer hijos recursivamente
                if (nodo.Hijos.Count > 0)
                {
                    await RestaurarNodosDesdeServidorAsync(nodo.Hijos);
                }
            }

            RebuildFailureSummary();
        }

        public void SelectHotspot(LevantamientoHotspotItem hotspot)
        {
            ArgumentNullException.ThrowIfNull(hotspot);

            foreach (var item in Hotspots)
            {
                item.IsSelected = ReferenceEquals(item, hotspot);
            }

            SelectedHotspot = hotspot;
        }

        #region Selector de equipo

        public async Task CargarEquiposAsync()
        {
            try
            {
                IsLoadingEquipos = true;
                _allEquipos = await _equipoService.GetEquiposAsync(null, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _allEquipos = new List<EquipoDto>();
                await _logger.LogErrorAsync("Error al cargar equipos para levantamiento", ex, nameof(LevantamientoViewModel), nameof(CargarEquiposAsync));
            }
            finally
            {
                IsLoadingEquipos = false;
            }
        }

        public void FiltrarEquipos(string? texto)
        {
            EquiposSugeridos.Clear();

            var searchText = texto?.Trim().ToLowerInvariant() ?? string.Empty;

            IEnumerable<EquipoDto> fuente = string.IsNullOrWhiteSpace(searchText)
                ? _allEquipos
                : _allEquipos.Where(e =>
                    (e.Identificador?.ToLowerInvariant().Contains(searchText) == true) ||
                    (e.Marca?.ToLowerInvariant().Contains(searchText) == true) ||
                    e.IdEquipo.ToString().Contains(searchText));

            foreach (var equipo in fuente.Take(10))
            {
                EquiposSugeridos.Add(FormatEquipoDisplay(equipo));
            }
        }

        public void SeleccionarEquipo(string? displayText)
        {
            if (string.IsNullOrWhiteSpace(displayText))
            {
                return;
            }

            var equipo = _allEquipos.FirstOrDefault(e => FormatEquipoDisplay(e) == displayText);
            EquipoSeleccionado = equipo;
        }

        public void LimpiarSeleccionEquipo()
        {
            EquipoSeleccionado = null;
        }

        private static string FormatEquipoDisplay(EquipoDto equipo)
        {
            return $"{equipo.Identificador} - {equipo.Marca}";
        }

        #endregion

        #region Guardar Levantamiento

        /// <summary>
        /// Guarda el levantamiento en el servidor y almacena el IdLevantamiento retornado.
        /// </summary>
        public async Task<(bool success, string message)> GuardarLevantamientoAsync(
            string? introduccion, string? conclusion, CancellationToken cancellationToken = default)
        {
            if (EquipoSeleccionado is null)
                return (false, "Debe seleccionar un equipo antes de guardar.");

            var capturedNodes = GetCapturedTree();
            if (capturedNodes.Count == 0)
                return (false, "No hay fallas registradas para guardar.");

            if (IsSaving)
                return (false, "Ya se esta guardando el levantamiento.");

            IsSaving = true;
            try
            {
                var nodosRequest = MapNodosToRequest(capturedNodes);
                LevantamientoResultResponse? result;

                if (IdLevantamiento > 0)
                {
                    // UPDATE: ya existe un levantamiento guardado
                    var updateRequest = new LevantamientoUpdateRequest
                    {
                        IdLevantamiento = IdLevantamiento,
                        Introduccion = introduccion?.Trim(),
                        Conclusion = conclusion?.Trim(),
                        TipoConfiguracion = "ElevadorDeTraccion",
                        Nodos = nodosRequest
                    };
                    result = await _levantamientoApiService.ActualizarLevantamientoAsync(updateRequest, cancellationToken);
                }
                else
                {
                    // CREATE: primer guardado
                    var createRequest = new LevantamientoCreateRequest
                    {
                        IdEquipo = EquipoSeleccionado.IdEquipo,
                        Introduccion = introduccion?.Trim(),
                        Conclusion = conclusion?.Trim(),
                        TipoConfiguracion = "ElevadorDeTraccion",
                        Nodos = nodosRequest
                    };
                    result = await _levantamientoApiService.CrearLevantamientoAsync(createRequest, cancellationToken);
                }

                if (result is null)
                    return (false, "No se recibio respuesta del servidor.");

                if (result.Success && result.IdLevantamiento > 0)
                {
                    IdLevantamiento = result.IdLevantamiento;
                    OnPropertyChanged(nameof(TieneLevantamientoGuardado));
                    await _logger.LogInformationAsync(
                        $"Levantamiento guardado exitosamente. Id: {IdLevantamiento}",
                        nameof(LevantamientoViewModel), nameof(GuardarLevantamientoAsync));
                    return (true, $"Levantamiento guardado exitosamente (Id: {IdLevantamiento}).");
                }

                if (result.Success)
                {
                    await _logger.LogInformationAsync(
                        $"Levantamiento actualizado exitosamente. Id: {IdLevantamiento}",
                        nameof(LevantamientoViewModel), nameof(GuardarLevantamientoAsync));
                    return (true, $"Levantamiento actualizado exitosamente (Id: {IdLevantamiento}).");
                }

                return (false, result.Message ?? "Error desconocido al guardar.");
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(
                    "Error al guardar levantamiento", ex,
                    nameof(LevantamientoViewModel), nameof(GuardarLevantamientoAsync));
                return (false, $"Error al guardar: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        private static List<LevantamientoNodoRequest> MapNodosToRequest(IReadOnlyList<LevantamientoNodoDto> nodos)
        {
            var result = new List<LevantamientoNodoRequest>(nodos.Count);
            foreach (var nodo in nodos)
            {
                result.Add(new LevantamientoNodoRequest
                {
                    Clave = nodo.Clave,
                    Etiqueta = nodo.Etiqueta,
                    DescripcionFalla = nodo.DescripcionFalla,
                    TieneFalla = nodo.TieneFalla,
                    Hijos = MapNodosToRequest(nodo.Hijos is IReadOnlyList<LevantamientoNodoDto> readOnly
                        ? readOnly
                        : nodo.Hijos.ToList())
                });
            }
            return result;
        }

        #endregion

        #region Generar Reporte

        /// <summary>
        /// Genera un reporte PDF del levantamiento y retorna la ruta del archivo.
        /// </summary>
        public async Task<string?> GenerarReporteAsync(string? introduccion, string? conclusion)
        {
            if (IdLevantamiento <= 0)
                return null;

            try
            {
                var filePath = await _reportService.GenerarReportePdfAsync(
                    IdLevantamiento,
                    EquipoSeleccionado?.Identificador,
                    EquipoSeleccionado?.Marca,
                    introduccion?.Trim(),
                    conclusion?.Trim(),
                    CapturedTreeItems);

                return filePath;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(
                    "Error al generar reporte PDF", ex,
                    nameof(LevantamientoViewModel), nameof(GenerarReporteAsync));
                throw;
            }
        }

        #endregion

        public void RegisterFailure(LevantamientoHotspotItem hotspot, string descripcion)
        {
            ArgumentNullException.ThrowIfNull(hotspot);
            ArgumentException.ThrowIfNullOrWhiteSpace(descripcion);

            SelectHotspot(hotspot);
            hotspot.DescripcionFalla = descripcion.Trim();
            UpdateReportNode(hotspot);
            RebuildFailureSummary();
        }

        public void AddImage(string hotspotKey, string imagePath)
        {
            var hotspot = TryGetHotspotByKey(hotspotKey)
                ?? throw new InvalidOperationException($"No se encontro el hotspot '{hotspotKey}'.");

            hotspot.AddImage(imagePath);
            UpdateReportNode(hotspot);
            RebuildFailureSummary();
        }

        public IReadOnlyList<string> RemoveFailure(string hotspotKey)
        {
            var hotspot = TryGetHotspotByKey(hotspotKey);
            if (hotspot is null)
            {
                return Array.Empty<string>();
            }

            var removedImages = hotspot.ClearImages();
            hotspot.DescripcionFalla = null;
            UpdateReportNode(hotspot);
            RebuildFailureSummary();
            return removedImages;
        }

        public LevantamientoHotspotItem? TryGetHotspotByVisualTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return null;
            }

            string hotspotKey;
            if (!_tagToHotspotKey.TryGetValue(tag, out hotspotKey!))
            {
                return null;
            }

            return Hotspots.FirstOrDefault(item => string.Equals(item.Clave, hotspotKey, StringComparison.Ordinal));
        }

        public LevantamientoHotspotItem? TryGetHotspotByKey(string hotspotKey)
        {
            if (string.IsNullOrWhiteSpace(hotspotKey))
            {
                return null;
            }

            return Hotspots.FirstOrDefault(item => string.Equals(item.Clave, hotspotKey, StringComparison.Ordinal));
        }

        public IReadOnlyList<LevantamientoNodoDto> GetCapturedTree()
        {
            return FilterNodesWithFailures(_reporte.Secciones);
        }

        private void BuildBaseState()
        {
            var definitions = BuildHotspotDefinitions();

            _tagToHotspotKey.Clear();
            foreach (var definition in definitions)
            {
                foreach (var tag in definition.VisualTags)
                {
                    _tagToHotspotKey[tag] = definition.Key;
                }
            }

            _reporte = BuildBaseReport(definitions);

            Hotspots.Clear();
            foreach (var hotspot in BuildHotspots(definitions))
            {
                Hotspots.Add(hotspot);
            }

            _isInitialized = true;
            if (Hotspots.Count > 0)
            {
                SelectHotspot(Hotspots[0]);
            }

            RebuildFailureSummary();
        }

        private void RebuildFailureSummary()
        {
            HotspotsConFalla.Clear();
            foreach (var hotspot in Hotspots.Where(item => item.TieneFalla))
            {
                HotspotsConFalla.Add(hotspot);
            }

            RebuildCapturedTreeItems();

            OnPropertyChanged(nameof(JsonLevantamiento));
        }

        private void RebuildCapturedTreeItems()
        {
            CapturedTreeItems.Clear();
            foreach (var node in GetCapturedTree())
            {
                CapturedTreeItems.Add(BuildTreeItem(node));
            }
        }

        private void UpdateReportNode(LevantamientoHotspotItem hotspot)
        {
            var node = FindNode(_reporte.Secciones, hotspot.Clave);
            if (node is null)
            {
                throw new InvalidOperationException($"No se encontro el nodo '{hotspot.Clave}' dentro del reporte base.");
            }

            node.DescripcionFalla = hotspot.DescripcionFalla;
            node.TieneFalla = hotspot.TieneFalla;
            node.Imagenes.Clear();
            foreach (var imagePath in hotspot.Imagenes)
            {
                node.Imagenes.Add(imagePath);
            }
        }

        private static IReadOnlyList<LevantamientoNodoDto> FilterNodesWithFailures(IEnumerable<LevantamientoNodoDto> nodes)
        {
            var filteredNodes = new List<LevantamientoNodoDto>();

            foreach (var node in nodes)
            {
                var filteredChildren = FilterNodesWithFailures(node.Hijos);
                if (!node.TieneFalla && filteredChildren.Count == 0)
                {
                    continue;
                }

                var clone = new LevantamientoNodoDto
                {
                    Clave = node.Clave,
                    Etiqueta = node.Etiqueta,
                    TieneFalla = node.TieneFalla,
                    DescripcionFalla = node.DescripcionFalla
                };

                foreach (var imagePath in node.Imagenes)
                {
                    clone.Imagenes.Add(imagePath);
                }

                foreach (var child in filteredChildren)
                {
                    clone.Hijos.Add(child);
                }

                filteredNodes.Add(clone);
            }

            return filteredNodes;
        }

        private static LevantamientoTreeItemModel BuildTreeItem(LevantamientoNodoDto node)
        {
            var treeItem = new LevantamientoTreeItemModel
            {
                Clave = node.Clave,
                Etiqueta = node.Etiqueta,
                DescripcionFalla = node.DescripcionFalla,
                TieneFalla = node.TieneFalla
            };

            // Restaurar imagenes del nodo en el arbol
            foreach (var imagePath in node.Imagenes)
            {
                var fileName = System.IO.Path.GetFileName(imagePath);
                treeItem.AddImage(new LevantamientoImageItem
                {
                    FileName = fileName,
                    FilePath = imagePath,
                    Title = fileName
                });
            }

            foreach (var child in node.Hijos)
            {
                treeItem.Hijos.Add(BuildTreeItem(child));
            }

            return treeItem;
        }

        private static LevantamientoNodoDto? FindNode(IEnumerable<LevantamientoNodoDto> nodes, string key)
        {
            foreach (var node in nodes)
            {
                if (string.Equals(node.Clave, key, StringComparison.Ordinal))
                {
                    return node;
                }

                var child = FindNode(node.Hijos, key);
                if (child is not null)
                {
                    return child;
                }
            }

            return null;
        }

        private static LevantamientoReporteDto BuildBaseReport(IReadOnlyList<HotspotDefinition> definitions)
        {
            var report = new LevantamientoReporteDto
            {
                Tipo = "levantamiento",
                Etiqueta = "elevador_de_traccion_con_cuarto_de_maquinas"
            };

            foreach (var definition in definitions)
            {
                var currentLevel = report.Secciones;
                foreach (var segment in definition.Hierarchy)
                {
                    var node = currentLevel.FirstOrDefault(item => string.Equals(item.Clave, segment.Key, StringComparison.Ordinal));
                    if (node is null)
                    {
                        node = new LevantamientoNodoDto
                        {
                            Clave = segment.Key,
                            Etiqueta = segment.Label
                        };

                        currentLevel.Add(node);
                    }

                    currentLevel = node.Hijos;
                }

                if (currentLevel.All(item => !string.Equals(item.Clave, definition.Key, StringComparison.Ordinal)))
                {
                    currentLevel.Add(new LevantamientoNodoDto
                    {
                        Clave = definition.Key,
                        Etiqueta = definition.Title
                    });
                }
            }

            return report;
        }

        private static IReadOnlyList<LevantamientoHotspotItem> BuildHotspots(IReadOnlyList<HotspotDefinition> definitions)
        {
            var hotspots = new List<LevantamientoHotspotItem>();
            foreach (var definition in definitions)
            {
                var hierarchy = definition.Hierarchy.Select(segment => segment.Key).ToList();
                hierarchy.Add(definition.Key);

                hotspots.Add(new LevantamientoHotspotItem
                {
                    Clave = definition.Key,
                    Titulo = definition.Title,
                    Seccion = definition.Section,
                    RutaJerarquica = hierarchy,
                    Left = 0,
                    Top = 0,
                    Width = 0,
                    Height = 0
                });
            }

            return hotspots;
        }

        private static IReadOnlyList<HotspotDefinition> BuildHotspotDefinitions()
        {
            return new List<HotspotDefinition>
            {
                CreateDefinition("SalaDeMaquinas", "Sala de maquinas", "Cuarto de maquinas / Sala y maniobra", new[] { "SalaMaquinas" }, "CuartoDeMaquinas", "Cuarto de maquinas", "SalaYManiobra", "Sala y maniobra"),
                CreateDefinition("ArmarioDeControl", "Armario de control", "Cuarto de maquinas / Sala y maniobra", new[] { "ArmarioManiobra" }, "CuartoDeMaquinas", "Cuarto de maquinas", "SalaYManiobra", "Sala y maniobra"),
                CreateDefinition("MaquinaDeTraccion", "Maquina de traccion", "Cuarto de maquinas / Maquina y traccion", new[] { "MaquinaTraccion" }, "CuartoDeMaquinas", "Cuarto de maquinas", "MaquinaYTraccion", "Maquina y traccion"),
                CreateDefinition("LimitadorDeVelocidad", "Limitador de velocidad", "Cuarto de maquinas / Seguridad y limitador", new[] { "LimitadorVelocidadVistaGeneral", "LimitadorVelocidadVistaLateral" }, "CuartoDeMaquinas", "Cuarto de maquinas", "SeguridadYLimitador", "Seguridad y limitador"),
                CreateDefinition("HuecoDelElevador", "Hueco del elevador", "Hueco del elevador / Estructura y guiado", new[] { "HuecoVistaGeneral" }, "HuecoDelElevadorRoot", "Hueco del elevador", "EstructuraYGuiado", "Estructura y guiado"),
                CreateDefinition("Guias", "Guias", "Hueco del elevador / Estructura y guiado", new[] { "GuiasVistaGeneral", "GuiaVistaLateral" }, "HuecoDelElevadorRoot", "Hueco del elevador", "EstructuraYGuiado", "Estructura y guiado"),
                CreateDefinition("Contrapeso", "Contrapeso", "Hueco del elevador / Estructura y guiado", new[] { "Contrapeso" }, "HuecoDelElevadorRoot", "Hueco del elevador", "EstructuraYGuiado", "Estructura y guiado"),
                CreateDefinition("CablesDeTraccion", "Cables de traccion", "Hueco del elevador / Suspension y compensacion", new[] { "CablesSuspension" }, "HuecoDelElevadorRoot", "Hueco del elevador", "SuspensionYCompensacion", "Suspension y compensacion"),
                CreateDefinition("CableDelLimitador", "Cable del limitador", "Hueco del elevador / Suspension y compensacion", new[] { "CableLimitadorVistaGeneral", "CableLimitadorVistaLateral" }, "HuecoDelElevadorRoot", "Hueco del elevador", "SuspensionYCompensacion", "Suspension y compensacion"),
                CreateDefinition("CadenaDeCompensacion", "Cadena de compensacion", "Hueco del elevador / Suspension y compensacion", new[] { "CadenaCompensacion" }, "HuecoDelElevadorRoot", "Hueco del elevador", "SuspensionYCompensacion", "Suspension y compensacion"),
                CreateDefinition("PuntoAnclajeContrapeso", "Punto de anclaje de contrapeso", "Hueco del elevador / Suspension y compensacion", new[] { "PuntoAnclajeContrapeso" }, "HuecoDelElevadorRoot", "Hueco del elevador", "SuspensionYCompensacion", "Suspension y compensacion"),
                CreateDefinition("GuiaDeCadena", "Guia de cadena", "Hueco del elevador / Suspension y compensacion", new[] { "GuiaCadena" }, "HuecoDelElevadorRoot", "Hueco del elevador", "SuspensionYCompensacion", "Suspension y compensacion"),
                CreateDefinition("ZonaDeBucleInferior", "Zona de bucle inferior", "Hueco del elevador / Suspension y compensacion", new[] { "ZonaBucleInferior" }, "HuecoDelElevadorRoot", "Hueco del elevador", "SuspensionYCompensacion", "Suspension y compensacion"),
                CreateDefinition("ProteccionDeCanal", "Proteccion de canal", "Hueco del elevador / Suspension y compensacion", new[] { "ProteccionCanal" }, "HuecoDelElevadorRoot", "Hueco del elevador", "SuspensionYCompensacion", "Suspension y compensacion"),
                CreateDefinition("PuertasDePiso", "Puertas de piso", "Hueco del elevador / Puertas de piso y seguridad", new[] { "PuertaPiso" }, "HuecoDelElevadorRoot", "Hueco del elevador", "PuertasDePisoYSeguridad", "Puertas de piso y seguridad"),
                CreateDefinition("EnclavamientoPuerta", "Enclavamiento de puerta", "Hueco del elevador / Puertas de piso y seguridad", new[] { "EnclavamientoPuerta" }, "HuecoDelElevadorRoot", "Hueco del elevador", "PuertasDePisoYSeguridad", "Puertas de piso y seguridad"),
                CreateDefinition("ContactosDeSeguridad", "Contactos de seguridad", "Hueco del elevador / Puertas de piso y seguridad", new[] { "ContactosSeguridad" }, "HuecoDelElevadorRoot", "Hueco del elevador", "PuertasDePisoYSeguridad", "Puertas de piso y seguridad"),
                CreateDefinition("BotoneraDePiso", "Botonera de piso", "Hueco del elevador / Puertas de piso y seguridad", new[] { "BotoneraPiso" }, "HuecoDelElevadorRoot", "Hueco del elevador", "PuertasDePisoYSeguridad", "Puertas de piso y seguridad"),
                CreateDefinition("DesbloqueoDeEmergencia", "Desbloqueo de emergencia", "Hueco del elevador / Puertas de piso y seguridad", new[] { "DesbloqueoEmergencia" }, "HuecoDelElevadorRoot", "Hueco del elevador", "PuertasDePisoYSeguridad", "Puertas de piso y seguridad"),
                CreateDefinition("Cabina", "Cabina", "Cabina y conjunto suspendido / Cabina y estructura", new[] { "Cabina" }, "CabinaYConjuntoSuspendido", "Cabina y conjunto suspendido", "CabinaYEstructura", "Cabina y estructura"),
                CreateDefinition("PuntoAnclajeCabina", "Punto de anclaje de cabina", "Cabina y conjunto suspendido / Cabina y estructura", new[] { "PuntoAnclajeCabina" }, "CabinaYConjuntoSuspendido", "Cabina y conjunto suspendido", "CabinaYEstructura", "Cabina y estructura"),
                CreateDefinition("Paracaidas", "Paracaidas", "Cabina y conjunto suspendido / Seguridad y guiado", new[] { "ParacaidasVistaGeneral", "ParacaidasVistaLateral" }, "CabinaYConjuntoSuspendido", "Cabina y conjunto suspendido", "SeguridadYGuiado", "Seguridad y guiado"),
                CreateDefinition("OperadorDePuerta", "Operador de puerta", "Cabina y conjunto suspendido / Puertas y umbral", new[] { "OperadorPuerta" }, "CabinaYConjuntoSuspendido", "Cabina y conjunto suspendido", "PuertasYUmbral", "Puertas y umbral"),
                CreateDefinition("PuertaDeCabina", "Puerta de cabina", "Cabina y conjunto suspendido / Puertas y umbral", new[] { "PuertaCabina" }, "CabinaYConjuntoSuspendido", "Cabina y conjunto suspendido", "PuertasYUmbral", "Puertas y umbral"),
                CreateDefinition("BarreraFotoelectrica", "Barrera fotoelectrica", "Cabina y conjunto suspendido / Puertas y umbral", new[] { "BarreraFotoelectrica" }, "CabinaYConjuntoSuspendido", "Cabina y conjunto suspendido", "PuertasYUmbral", "Puertas y umbral"),
                CreateDefinition("Umbral", "Umbral", "Cabina y conjunto suspendido / Puertas y umbral", new[] { "Umbral" }, "CabinaYConjuntoSuspendido", "Cabina y conjunto suspendido", "PuertasYUmbral", "Puertas y umbral"),
                CreateDefinition("IndicadorDeCabina", "Indicador de cabina", "Cabina y conjunto suspendido / Control e interfaz", new[] { "Indicador" }, "CabinaYConjuntoSuspendido", "Cabina y conjunto suspendido", "ControlEInterfaz", "Control e interfaz"),
                CreateDefinition("BotoneraDeCabina", "Botonera de cabina", "Cabina y conjunto suspendido / Control e interfaz", new[] { "BotoneraCabina" }, "CabinaYConjuntoSuspendido", "Cabina y conjunto suspendido", "ControlEInterfaz", "Control e interfaz"),
                CreateDefinition("Amortiguadores", "Amortiguadores", "Foso / Seguridad inferior", new[] { "Amortiguadores" }, "Foso", "Foso", "SeguridadInferior", "Seguridad inferior"),
                CreateDefinition("TensorDelLimitador", "Tensor del limitador", "Foso / Limitador y tension", new[] { "TensorLimitadorVistaGeneral", "TensorLimitadorVistaLateral" }, "Foso", "Foso", "LimitadorYTension", "Limitador y tension")
            };
        }

        private static HotspotDefinition CreateDefinition(
            string key,
            string title,
            string section,
            IReadOnlyList<string> visualTags,
            string parentKey,
            string parentLabel,
            string groupKey,
            string groupLabel)
        {
            return new HotspotDefinition
            {
                Key = key,
                Title = title,
                Section = section,
                VisualTags = visualTags,
                Hierarchy = new List<HierarchySegment>
                {
                    new HierarchySegment { Key = parentKey, Label = parentLabel },
                    new HierarchySegment { Key = groupKey, Label = groupLabel }
                }
            };
        }

        private sealed class HierarchySegment
        {
            public string Key { get; set; } = string.Empty;

            public string Label { get; set; } = string.Empty;
        }

        private sealed class HotspotDefinition
        {
            public string Key { get; set; } = string.Empty;

            public string Title { get; set; } = string.Empty;

            public string Section { get; set; } = string.Empty;

            public IReadOnlyList<string> VisualTags { get; set; } = Array.Empty<string>();

            public IReadOnlyList<HierarchySegment> Hierarchy { get; set; } = Array.Empty<HierarchySegment>();
        }
    }
}
