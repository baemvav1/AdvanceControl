using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Logging;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Advance_Control.ViewModels
{
    public class LevantamientoViewModel : ViewModelBase
    {
        private const double BaseImageWidth = 1152d;
        private const double BaseImageHeight = 928d;

        private readonly ILoggingService _logger;
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
        private bool _isLoading;
        private string? _errorMessage;
        private bool _isInitialized;
        private LevantamientoHotspotItem? _selectedHotspot;
        private LevantamientoReporteDto _reporte = new();

        public LevantamientoViewModel(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Hotspots = new ObservableCollection<LevantamientoHotspotItem>();
            HotspotsConFalla = new ObservableCollection<LevantamientoHotspotItem>();
            HotspotImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Images/ImagenHotSpotElevadorDeTraccion.jpg"));
            BuildBaseState();
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ObservableCollection<LevantamientoHotspotItem> Hotspots { get; }

        public ObservableCollection<LevantamientoHotspotItem> HotspotsConFalla { get; }

        public BitmapImage HotspotImageSource { get; }

        public double ImageWidth => BaseImageWidth;

        public double ImageHeight => BaseImageHeight;

        public LevantamientoHotspotItem? SelectedHotspot
        {
            get => _selectedHotspot;
            private set
            {
                if (SetProperty(ref _selectedHotspot, value))
                {
                    OnPropertyChanged(nameof(SelectedHotspotTitle));
                    OnPropertyChanged(nameof(SelectedHotspotSection));
                    OnPropertyChanged(nameof(SelectedHotspotDescription));
                }
            }
        }

        public string SelectedHotspotTitle => SelectedHotspot?.Titulo ?? "Selecciona un componente";

        public string SelectedHotspotSection => SelectedHotspot?.Seccion ?? "Sin componente seleccionado";

        public string SelectedHotspotDescription => SelectedHotspot?.DescripcionResumen ?? "Aun no se ha capturado una falla.";

        public string JsonLevantamiento => JsonSerializer.Serialize(_reporte, _jsonOptions);

        public string ResumenCapturas => HotspotsConFalla.Count == 0
            ? "Aun no se han capturado fallas."
            : $"Se han capturado {HotspotsConFalla.Count} falla(s).";

        public string ResumenFallasDetalle => HotspotsConFalla.Count == 0
            ? "Aun no se han capturado fallas."
            : string.Join(Environment.NewLine + Environment.NewLine, HotspotsConFalla.Select(hotspot => $"{hotspot.Titulo} ({hotspot.Seccion}){Environment.NewLine}{hotspot.DescripcionResumen}"));

        public LevantamientoHotspotItem SalaDeMaquinasHotspot { get; private set; } = null!;
        public LevantamientoHotspotItem MaquinaDeTraccionHotspot { get; private set; } = null!;
        public LevantamientoHotspotItem LimitadorDeVelocidadHotspot { get; private set; } = null!;
        public LevantamientoHotspotItem CabinaHotspot { get; private set; } = null!;
        public LevantamientoHotspotItem ContrapesoHotspot { get; private set; } = null!;
        public LevantamientoHotspotItem ParacaidasHotspot { get; private set; } = null!;
        public LevantamientoHotspotItem PuertaCabinaOperacionHotspot { get; private set; } = null!;
        public LevantamientoHotspotItem BotoneraCabinaHotspot { get; private set; } = null!;
        public LevantamientoHotspotItem PuertaDePisoHotspot { get; private set; } = null!;
        public LevantamientoHotspotItem EnclavamientoYContactosHotspot { get; private set; } = null!;
        public LevantamientoHotspotItem CadenaDeCompensacionHotspot { get; private set; } = null!;

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

                await _logger.LogInformationAsync("Levantamiento inicializado", nameof(LevantamientoViewModel), nameof(InitializeAsync));
            }
            catch (Exception ex)
            {
                ErrorMessage = "No se pudo inicializar el módulo de levantamiento.";
                await _logger.LogErrorAsync("Error al inicializar levantamiento", ex, nameof(LevantamientoViewModel), nameof(InitializeAsync));
            }
            finally
            {
                IsLoading = false;
            }
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

        public void RegisterFailure(LevantamientoHotspotItem hotspot, string descripcion)
        {
            ArgumentNullException.ThrowIfNull(hotspot);
            ArgumentException.ThrowIfNullOrWhiteSpace(descripcion);

            SelectHotspot(hotspot);
            hotspot.DescripcionFalla = descripcion.Trim();
            UpdateReportNode(hotspot);
            RebuildFailureSummary();
        }

        public LevantamientoHotspotItem? TryGetHotspot(string key)
        {
            return Hotspots.FirstOrDefault(item => string.Equals(item.Clave, key, StringComparison.Ordinal));
        }

        private void BuildBaseState()
        {
            _reporte = BuildBaseReport();

            var hotspots = BuildHotspots();
            AssignHotspotReferences(hotspots);

            Hotspots.Clear();
            foreach (var hotspot in hotspots)
            {
                Hotspots.Add(hotspot);
            }

            _isInitialized = true;
            if (Hotspots.Count > 0)
            {
                SelectHotspot(Hotspots[0]);
            }

            RebuildFailureSummary();
            OnPropertyChanged(nameof(JsonLevantamiento));
        }

        private void AssignHotspotReferences(IReadOnlyList<LevantamientoHotspotItem> hotspots)
        {
            var hotspotsByKey = hotspots.ToDictionary(item => item.Clave, StringComparer.Ordinal);

            SalaDeMaquinasHotspot = hotspotsByKey["SalaDeMaquinas"];
            MaquinaDeTraccionHotspot = hotspotsByKey["MaquinaDeTraccion"];
            LimitadorDeVelocidadHotspot = hotspotsByKey["LimitadorDeVelocidad"];
            CabinaHotspot = hotspotsByKey["Cabina"];
            ContrapesoHotspot = hotspotsByKey["Contrapeso"];
            ParacaidasHotspot = hotspotsByKey["Paracaidas"];
            PuertaCabinaOperacionHotspot = hotspotsByKey["PuertaCabinaOperacion"];
            BotoneraCabinaHotspot = hotspotsByKey["BotoneraCabina"];
            PuertaDePisoHotspot = hotspotsByKey["PuertaDePiso"];
            EnclavamientoYContactosHotspot = hotspotsByKey["EnclavamientoYContactos"];
            CadenaDeCompensacionHotspot = hotspotsByKey["CadenaDeCompensacion"];
        }

        private void RebuildFailureSummary()
        {
            HotspotsConFalla.Clear();
            foreach (var hotspot in Hotspots.Where(item => item.TieneFalla))
            {
                HotspotsConFalla.Add(hotspot);
            }

            OnPropertyChanged(nameof(ResumenCapturas));
            OnPropertyChanged(nameof(ResumenFallasDetalle));
            OnPropertyChanged(nameof(JsonLevantamiento));
            OnPropertyChanged(nameof(SelectedHotspotDescription));
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

        private static LevantamientoReporteDto BuildBaseReport()
        {
            return new LevantamientoReporteDto
            {
                Secciones =
                {
                    new LevantamientoNodoDto
                    {
                        Clave = "VistaGeneral",
                        Etiqueta = "Vista general",
                        Hijos =
                        {
                            new LevantamientoNodoDto { Clave = "SalaDeMaquinas", Etiqueta = "Sala de maquinas" },
                            new LevantamientoNodoDto { Clave = "MaquinaDeTraccion", Etiqueta = "Maquina de traccion" },
                            new LevantamientoNodoDto { Clave = "LimitadorDeVelocidad", Etiqueta = "Limitador de velocidad" },
                            new LevantamientoNodoDto { Clave = "Cabina", Etiqueta = "Cabina" },
                            new LevantamientoNodoDto { Clave = "Contrapeso", Etiqueta = "Contrapeso" },
                            new LevantamientoNodoDto { Clave = "Paracaidas", Etiqueta = "Paracaidas" }
                        }
                    },
                    new LevantamientoNodoDto
                    {
                        Clave = "CabinaSeguridad",
                        Etiqueta = "Cabina y seguridad",
                        Hijos =
                        {
                            new LevantamientoNodoDto { Clave = "PuertaCabinaOperacion", Etiqueta = "Puerta de cabina" },
                            new LevantamientoNodoDto { Clave = "BotoneraCabina", Etiqueta = "Botonera de cabina" }
                        }
                    },
                    new LevantamientoNodoDto
                    {
                        Clave = "PuertaPiso",
                        Etiqueta = "Puerta de piso",
                        Hijos =
                        {
                            new LevantamientoNodoDto { Clave = "PuertaDePiso", Etiqueta = "Puerta de piso" },
                            new LevantamientoNodoDto { Clave = "EnclavamientoYContactos", Etiqueta = "Enclavamiento y contactos de seguridad" }
                        }
                    },
                    new LevantamientoNodoDto
                    {
                        Clave = "Compensacion",
                        Etiqueta = "Compensacion",
                        Hijos =
                        {
                            new LevantamientoNodoDto { Clave = "CadenaDeCompensacion", Etiqueta = "Cadena de compensacion" }
                        }
                    }
                }
            };
        }

        private static IReadOnlyList<LevantamientoHotspotItem> BuildHotspots()
        {
            return
            [
                CreateHotspot("SalaDeMaquinas", "Sala de maquinas", "Vista general", 0.115, 0.055, 0.355, 0.180, ["VistaGeneral", "SalaDeMaquinas"]),
                CreateHotspot("MaquinaDeTraccion", "Maquina de traccion", "Vista general", 0.205, 0.135, 0.185, 0.130, ["VistaGeneral", "MaquinaDeTraccion"]),
                CreateHotspot("LimitadorDeVelocidad", "Limitador de velocidad", "Vista general", 0.392, 0.120, 0.115, 0.140, ["VistaGeneral", "LimitadorDeVelocidad"]),
                CreateHotspot("Cabina", "Cabina", "Vista general", 0.150, 0.520, 0.225, 0.180, ["VistaGeneral", "Cabina"]),
                CreateHotspot("Contrapeso", "Contrapeso", "Vista general", 0.409, 0.560, 0.072, 0.145, ["VistaGeneral", "Contrapeso"]),
                CreateHotspot("Paracaidas", "Paracaidas", "Vista general", 0.428, 0.487, 0.082, 0.085, ["VistaGeneral", "Paracaidas"]),
                CreateHotspot("PuertaCabinaOperacion", "Puerta de cabina", "Cabina y seguridad", 0.587, 0.100, 0.141, 0.255, ["CabinaSeguridad", "PuertaCabinaOperacion"]),
                CreateHotspot("BotoneraCabina", "Botonera de cabina", "Cabina y seguridad", 0.740, 0.185, 0.053, 0.115, ["CabinaSeguridad", "BotoneraCabina"]),
                CreateHotspot("PuertaDePiso", "Puerta de piso", "Puerta de piso", 0.605, 0.505, 0.218, 0.140, ["PuertaPiso", "PuertaDePiso"]),
                CreateHotspot("EnclavamientoYContactos", "Enclavamiento y contactos", "Puerta de piso", 0.793, 0.435, 0.100, 0.125, ["PuertaPiso", "EnclavamientoYContactos"]),
                CreateHotspot("CadenaDeCompensacion", "Cadena de compensacion", "Compensacion", 0.632, 0.815, 0.150, 0.108, ["Compensacion", "CadenaDeCompensacion"])
            ];
        }

        private static LevantamientoHotspotItem CreateHotspot(
            string key,
            string title,
            string section,
            double relativeLeft,
            double relativeTop,
            double relativeWidth,
            double relativeHeight,
            IReadOnlyList<string> hierarchy)
        {
            return new LevantamientoHotspotItem
            {
                Clave = key,
                Titulo = title,
                Seccion = section,
                RutaJerarquica = hierarchy,
                Left = relativeLeft * BaseImageWidth,
                Top = relativeTop * BaseImageHeight,
                Width = relativeWidth * BaseImageWidth,
                Height = relativeHeight * BaseImageHeight
            };
        }
    }
}
