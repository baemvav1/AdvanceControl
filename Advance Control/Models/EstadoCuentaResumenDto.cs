using System;
using System.Globalization;
using System.Linq;

namespace Advance_Control.Models
{
    public class EstadoCuentaResumenDto
    {
        public int IdEstadoCuenta { get; set; }
        public string NumeroCuenta { get; set; } = string.Empty;
        public string Clabe { get; set; } = string.Empty;
        public string? TipoCuenta { get; set; }
        public string? TipoMoneda { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public DateTime FechaCorte { get; set; }
        public decimal SaldoInicial { get; set; }
        public decimal TotalCargos { get; set; }
        public decimal TotalAbonos { get; set; }
        public decimal SaldoFinal { get; set; }
        public decimal TotalComisiones { get; set; }
        public decimal TotalISR { get; set; }
        public decimal TotalIVA { get; set; }
        public int TotalTransaccionesIndividuales { get; set; }
        public int TotalGrupos { get; set; }
        public string? VersionXml { get; set; }
        public string? NombreBanco { get; set; }
        public string? NombreSucursal { get; set; }
        public string? Titular { get; set; }
        public string? NumeroCliente { get; set; }
        public string? FolioFiscal { get; set; }
        public DateTime? FechaCarga { get; set; }
        public string? ReferenciasBusqueda { get; set; }

        public string CuentaTitulo => string.IsNullOrWhiteSpace(TipoCuenta)
            ? NumeroCuenta
            : $"{NumeroCuenta} · {TipoCuenta}";

        public string PeriodoTexto => $"{FechaInicio:dd/MM/yyyy} - {FechaFin:dd/MM/yyyy}";
        public string FechaCorteTexto => FechaCorte == default ? string.Empty : FechaCorte.ToString("dd/MM/yyyy");
        public string SaldoFinalTexto => SaldoFinal.ToString("C2", new CultureInfo("es-MX"));
        public string SaldoInicialTexto => SaldoInicial.ToString("C2", new CultureInfo("es-MX"));
        public string TotalCargosTexto => TotalCargos.ToString("C2", new CultureInfo("es-MX"));
        public string TotalAbonosTexto => TotalAbonos.ToString("C2", new CultureInfo("es-MX"));
        public string TotalComisionesTexto => TotalComisiones.ToString("C2", new CultureInfo("es-MX"));
        public string TotalImpuestosTexto => (TotalISR + TotalIVA).ToString("C2", new CultureInfo("es-MX"));
        public string FechaCargaTexto => FechaCarga.HasValue ? FechaCarga.Value.ToString("dd/MM/yyyy HH:mm") : "Sin fecha de carga";
        public string BancoTitularTexto => string.Join(" · ", new[] { NombreBanco, Titular }.Where(v => !string.IsNullOrWhiteSpace(v)));
        public string TotalesEstructuraTexto => $"Version {VersionXml ?? "2.0"} · Grupos: {TotalGrupos} · Individuales: {TotalTransaccionesIndividuales}";
        public string FolioFiscalTexto => string.IsNullOrWhiteSpace(FolioFiscal) ? "Sin folio fiscal" : FolioFiscal!;
    }
}
