using System;
using System.Collections.Generic;
using System.Globalization;

namespace Advance_Control.Models
{
    public class ReporteFinancieroFacturacionCabeceraDto
    {
        public string? ReceptorRfc { get; set; }
        public string? ReceptorNombre { get; set; }
        public int NumeroFacturas { get; set; }
        public int NumeroFacturasFiniquitadas { get; set; }
        public int NumeroFacturasNoFiniquitadas { get; set; }
        public decimal TotalFacturado { get; set; }
        public decimal TotalAbonadoMovimientos { get; set; }

        public string ReceptorRfcTexto => string.IsNullOrWhiteSpace(ReceptorRfc) ? "Sin RFC" : ReceptorRfc!;
        public string ReceptorNombreTexto => string.IsNullOrWhiteSpace(ReceptorNombre) ? "Sin nombre" : ReceptorNombre!;
        public string NumeroFacturasTexto => NumeroFacturas.ToString(CultureInfo.InvariantCulture);
        public string NumeroFacturasFiniquitadasTexto => NumeroFacturasFiniquitadas.ToString(CultureInfo.InvariantCulture);
        public string NumeroFacturasNoFiniquitadasTexto => NumeroFacturasNoFiniquitadas.ToString(CultureInfo.InvariantCulture);
        public string TotalFacturadoTexto => TotalFacturado.ToString("C2", new CultureInfo("es-MX"));
        public string TotalAbonadoMovimientosTexto => TotalAbonadoMovimientos.ToString("C2", new CultureInfo("es-MX"));
        public string ResumenTexto => $"{NumeroFacturas} factura(s) · {NumeroFacturasFiniquitadas} finiquitada(s) · {NumeroFacturasNoFiniquitadas} pendiente(s)";
    }

    public class ReporteFinancieroFacturacionDetalleDto
    {
        public string? Folio { get; set; }
        public DateTime? FechaTimbrado { get; set; }
        public decimal Total { get; set; }
        public bool? Finiquito { get; set; }
        public string? EmisorRfc { get; set; }
        public string? ReceptorRfc { get; set; }
        public string? ReceptorNombre { get; set; }
        public string? TipoOperacion { get; set; }
        public decimal? Abono { get; set; }
        public string? Referencia { get; set; }

        public string FolioTexto => string.IsNullOrWhiteSpace(Folio) ? "Sin folio" : Folio!;
        public string FechaTimbradoTexto => FechaTimbrado.HasValue ? FechaTimbrado.Value.ToString("dd/MM/yyyy HH:mm") : "Sin fecha";
        public string TotalTexto => Total.ToString("C2", new CultureInfo("es-MX"));
        public string AbonoTexto => (Abono ?? 0m).ToString("C2", new CultureInfo("es-MX"));
        public string EstadoTexto => Finiquito == true ? "Finiquitada" : "Pendiente";
        public string EmisorRfcTexto => string.IsNullOrWhiteSpace(EmisorRfc) ? "Sin RFC emisor" : EmisorRfc!;
        public string ReceptorRfcTexto => string.IsNullOrWhiteSpace(ReceptorRfc) ? "Sin RFC receptor" : ReceptorRfc!;
        public string ReceptorNombreTexto => string.IsNullOrWhiteSpace(ReceptorNombre) ? "Sin receptor" : ReceptorNombre!;
        public string TipoOperacionTexto => string.IsNullOrWhiteSpace(TipoOperacion) ? "Sin tipo de operación" : TipoOperacion!;
        public string ReferenciaTexto => string.IsNullOrWhiteSpace(Referencia) ? "Sin referencia" : Referencia!;
        public string ResumenSecundario => $"{FechaTimbradoTexto} · {TipoOperacionTexto} · {EstadoTexto}";
    }

    public class ReporteFinancieroFacturacionResponseDto
    {
        public List<ReporteFinancieroFacturacionCabeceraDto> Cabeceras { get; set; } = new();
        public List<ReporteFinancieroFacturacionDetalleDto> Detalles { get; set; } = new();
    }

    public class ReporteFinancieroFacturacionListadoItemDto
    {
        public ReporteFinancieroFacturacionCabeceraDto? Cabecera { get; set; }
        public ReporteFinancieroFacturacionDetalleDto? Detalle { get; set; }

        public bool EsCabecera => Cabecera != null;
        public bool EsDetalle => Detalle != null;
    }
}
