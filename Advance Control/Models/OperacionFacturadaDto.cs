using System;
using System.Globalization;

namespace Advance_Control.Models
{
    public class OperacionFacturadaDto
    {
        public int IdOperacion { get; set; }
        /// <summary>Número visible ("R..." si es recuperada). Migración 142.</summary>
        public string? FolioOperacion { get; set; }
        public string? TipoMantenimiento { get; set; }
        public string? RazonSocial { get; set; }
        public string? Identificador { get; set; }
        public string? Atiende { get; set; }
        public long? IdAtiende { get; set; }
        public double Monto { get; set; }
        public DateTime? FechaFinal { get; set; }
        public int IdFactura { get; set; }
        public string? Folio { get; set; }
        public string? Uuid { get; set; }
        public decimal Total { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime? FechaTimbrado { get; set; }
        public decimal TotalAbonado { get; set; }
        public long NumeroAbonos { get; set; }

        public string TotalTexto => Total.ToString("C2", new CultureInfo("es-MX"));
        public string FechaTexto => Fecha.ToString("dd/MM/yyyy");
        public string UuidTexto => string.IsNullOrWhiteSpace(Uuid) ? "Sin UUID" : $"UUID: {Uuid}";
        public string AtiendeTexto => string.IsNullOrWhiteSpace(Atiende) ? "Sin asignar" : Atiende;
        public string FolioTexto => string.IsNullOrWhiteSpace(FolioOperacion) ? IdOperacion.ToString() : FolioOperacion!;
        public string IdOperacionTexto => $"Operación #{FolioTexto}";
        /// <summary>Check "Orden de compra cargada" de la operación. Migración 144.</summary>
        public bool OrdenCompraCargada { get; set; }
        public string OrdenCompraTexto => OrdenCompraCargada ? "Con orden de compra" : "Sin orden de compra";
        public bool TieneAbonos => NumeroAbonos > 0;
    }
}
