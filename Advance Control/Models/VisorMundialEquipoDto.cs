using System;

namespace Advance_Control.Models
{
    public class VisorMundialEquipoDto
    {
        public int IdEquipo { get; set; }
        public string? Identificador { get; set; }
        public string? Marca { get; set; }
        public string? Descripcion { get; set; }
        public string? Clientes { get; set; }
        public int? IdOperacionAbierta { get; set; }
        public string? TipoMantenimientoAbierta { get; set; }
        public DateTime? FechaInicioAbierta { get; set; }

        public string ClientesTexto => string.IsNullOrWhiteSpace(Clientes) ? "Sin cliente asociado" : Clientes;
        public bool TieneOperacionAbierta => IdOperacionAbierta.HasValue;
        public string EstadoTexto => TieneOperacionAbierta ? $"Operación abierta: {TipoMantenimientoAbierta}" : "Sin operación abierta";
    }
}
