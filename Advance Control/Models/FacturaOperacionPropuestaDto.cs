using System.Collections.Generic;

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
    }
}
