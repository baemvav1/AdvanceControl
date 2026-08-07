using System.Collections.Generic;
using Advance_Control.Services.Facturacion;

namespace Advance_Control.Models
{
    public sealed class FacturaOperacionPropuestaDto
    {
        private FacturaResumenDto? _facturaSeleccionada;

        public required OperacionSinFacturaDto Operacion { get; init; }
        public required List<FacturaResumenDto> Candidatas { get; init; }
        public bool Aprobado { get; set; } = true;

        public FacturaResumenDto FacturaSeleccionada
        {
            get => _facturaSeleccionada ??= Candidatas[0];
            set => _facturaSeleccionada = value;
        }

        public bool TieneMultiplesCandidatas => Candidatas.Count > 1;

        /// <summary>
        /// True si la factura seleccionada menciona el numero de esta operacion en
        /// "condiciones_de_pago" (cotizacion/reporte) -- coincidencia mas confiable que
        /// solo RFC+monto. Se recalcula sobre la seleccion actual, no queda fija.
        /// </summary>
        public bool CoincidePorNumeroCotizacion =>
            FacturaOperacionMatchingEngine.ReferenciaContieneOperacion(FacturaSeleccionada.CondicionesDePago, Operacion.IdOperacion);
    }
}
