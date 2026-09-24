using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Facturas;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// Auditoría de todos los Complementos de Pago del sistema (los generados por la app y los
    /// históricos backfilleados del portal de Bilkon): a qué factura corresponde cada uno, y
    /// cuáles quedan "huérfanos" (un documento relacionado cuyo UUID no matchea ninguna factura
    /// propia). El listado es por documento relacionado (fn_complementos_pago_listar), no por
    /// complemento -- un mismo complemento puede tener un docto con match y otro huérfano.
    /// </summary>
    public class ComplementosPagoAuditoriaViewModel : ViewModelBase
    {
        private readonly IFacturaService _facturaService;
        private readonly IComplementoPagoPdfService _complementoPagoPdfService;
        private bool _isLoading;
        private string? _errorMessage;
        private bool _soloHuerfanos;

        public ComplementosPagoAuditoriaViewModel(IFacturaService facturaService, IComplementoPagoPdfService complementoPagoPdfService)
        {
            _facturaService = facturaService ?? throw new ArgumentNullException(nameof(facturaService));
            _complementoPagoPdfService = complementoPagoPdfService ?? throw new ArgumentNullException(nameof(complementoPagoPdfService));
            Renglones = new ObservableCollection<ComplementoPagoResumenDto>();
        }

        public ObservableCollection<ComplementoPagoResumenDto> Renglones { get; }

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                if (SetProperty(ref _errorMessage, value))
                    OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public bool SoloHuerfanos
        {
            get => _soloHuerfanos;
            set
            {
                if (SetProperty(ref _soloHuerfanos, value))
                {
                    _ = CargarAsync();
                }
            }
        }

        public int TotalComplementos => Renglones.Select(r => r.IdFacturaComplemento).Distinct().Count();
        public int TotalHuerfanos => Renglones.Where(r => !r.Matched).Select(r => r.IdFacturaComplemento).Distinct().Count();
        public string ResumenTexto => $"{TotalComplementos} complemento(s) · {TotalHuerfanos} con al menos un documento huérfano";

        public async Task CargarAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var resultado = await _facturaService.ObtenerComplementosPagoAsync(SoloHuerfanos);

                Renglones.Clear();
                foreach (var renglon in resultado)
                {
                    Renglones.Add(renglon);
                }

                OnPropertyChanged(nameof(TotalComplementos));
                OnPropertyChanged(nameof(TotalHuerfanos));
                OnPropertyChanged(nameof(ResumenTexto));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar los complementos de pago: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task<string?> GenerarPdfAsync(int idFacturaComplemento)
        {
            try
            {
                ErrorMessage = null;
                var detalle = await _facturaService.ObtenerComplementoPagoDetalleAsync(idFacturaComplemento);
                if (detalle?.Factura == null)
                {
                    ErrorMessage = "No se encontró el complemento de pago seleccionado.";
                    return null;
                }

                return await _complementoPagoPdfService.GenerarComplementoPagoPdfAsync(detalle);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al generar el PDF del complemento de pago: {ex.Message}";
                return null;
            }
        }
    }
}
