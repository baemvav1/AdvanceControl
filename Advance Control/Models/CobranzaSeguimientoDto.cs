using System;
using System.Globalization;

namespace Advance_Control.Models
{
    public class CobranzaSeguimientoCreateDto
    {
        public string? ReceptorRfc { get; set; }
        public int? IdFactura { get; set; }
        public string? TipoContacto { get; set; }
        public string? Nota { get; set; }
        public decimal? PromesaPagoMonto { get; set; }
        public DateTime? PromesaPagoFecha { get; set; }
    }

    public class CobranzaSeguimientoDto
    {
        public long Id { get; set; }
        public string? ReceptorRfc { get; set; }
        public int? IdFactura { get; set; }
        public long CredencialId { get; set; }
        public string? RegistradoPor { get; set; }
        public string? TipoContacto { get; set; }
        public string? Nota { get; set; }
        public decimal? PromesaPagoMonto { get; set; }
        public DateTime? PromesaPagoFecha { get; set; }
        public DateTime FechaSeguimiento { get; set; }

        public string FechaSeguimientoTexto => FechaSeguimiento.ToString("dd/MM/yyyy HH:mm");
        public string RegistradoPorTexto => string.IsNullOrWhiteSpace(RegistradoPor) ? "Usuario desconocido" : RegistradoPor!;
        public string TipoContactoTexto => string.IsNullOrWhiteSpace(TipoContacto) ? "Sin tipo" : TipoContacto!;
        public string NotaTexto => string.IsNullOrWhiteSpace(Nota) ? "Sin nota" : Nota!;
        public string PromesaTexto => PromesaPagoFecha.HasValue
            ? $"Promete pagar {(PromesaPagoMonto ?? 0m).ToString("C2", new CultureInfo("es-MX"))} el {PromesaPagoFecha.Value:dd/MM/yyyy}"
            : "Sin promesa de pago";
    }
}
