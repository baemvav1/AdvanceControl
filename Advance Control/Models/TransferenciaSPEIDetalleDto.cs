using System;
using System.Globalization;

namespace Advance_Control.Models
{
    public class TransferenciaSPEIDetalleDto
    {
        public int IdTransferencia { get; set; }
        public int IdMovimiento { get; set; }
        public string TipoTransferencia { get; set; } = string.Empty;
        public string? BancoClave { get; set; }
        public string? BancoNombre { get; set; }
        public string? CuentaOrigen { get; set; }
        public string? CuentaDestino { get; set; }
        public string? NombreEmisor { get; set; }
        public string? NombreDestinatario { get; set; }
        public string? RfcEmisor { get; set; }
        public string? RfcDestinatario { get; set; }
        public string? ClaveRastreo { get; set; }
        public string? Concepto { get; set; }
        public TimeSpan? Hora { get; set; }
        public decimal Monto { get; set; }
        public DateTime? Fecha { get; set; }
        public string? Referencia { get; set; }

        public string FechaTexto => Fecha.HasValue ? Fecha.Value.ToString("dd/MM/yyyy") : string.Empty;

        public string MontoTexto => Monto.ToString("C2", new CultureInfo("es-MX"));

        public string HoraTexto => Hora.HasValue ? Hora.Value.ToString(@"hh\:mm\:ss") : string.Empty;
    }
}
