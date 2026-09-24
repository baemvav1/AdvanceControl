using System;
using System.Collections.Generic;
using System.Globalization;
using Advance_Control.Utilities;

namespace Advance_Control.Models
{
    /// <summary>Un renglón (un DoctoRelacionado) de la auditoría de complementos de pago.</summary>
    public class ComplementoPagoResumenDto
    {
        public int IdFacturaComplemento { get; set; }
        public string? ComplementoSerie { get; set; }
        public string? ComplementoFolio { get; set; }
        public string? ComplementoUuid { get; set; }
        public DateTime? ComplementoFecha { get; set; }
        public string? ComplementoReceptorRfc { get; set; }
        public string? ComplementoReceptorNombre { get; set; }
        public int IdPago { get; set; }
        public DateTime FechaPago { get; set; }
        public string? FormaPago { get; set; }
        public int NumParcialidad { get; set; }
        public decimal ImpSaldoAnt { get; set; }
        public decimal ImpPagado { get; set; }
        public decimal ImpSaldoInsoluto { get; set; }
        public string UuidDoctoRelacionado { get; set; } = string.Empty;
        public string? FolioDoctoRelacionado { get; set; }
        public int? IdFacturaPagada { get; set; }
        public string? FacturaPagadaSerie { get; set; }
        public string? FacturaPagadaFolio { get; set; }
        public bool Matched { get; set; }

        public string ComplementoFolioTitulo => string.IsNullOrWhiteSpace(ComplementoSerie) ? $"{ComplementoFolio}" : $"{ComplementoSerie}{ComplementoFolio}";
        public string FacturaPagadaTexto => Matched
            ? (string.IsNullOrWhiteSpace(FacturaPagadaSerie) ? $"Folio {FacturaPagadaFolio}" : $"{FacturaPagadaSerie}{FacturaPagadaFolio}")
            : $"Sin match — UUID: {UuidDoctoRelacionado}";
        public string EstadoTexto => Matched ? "OK" : "Huérfano";
        public string ImpPagadoTexto => ImpPagado.ToString("C2", new CultureInfo("es-MX"));
    }

    /// <summary>Complemento de Pago que cita una factura como documento pagado (relación inversa).</summary>
    public class ComplementoPagoRelacionadoDto
    {
        public int IdFacturaComplemento { get; set; }
        public string? Serie { get; set; }
        public string? Folio { get; set; }
        public string? Uuid { get; set; }
        public DateTime? FechaTimbrado { get; set; }
        public DateTime FechaPago { get; set; }
        public string? FormaPago { get; set; }
        public string Moneda { get; set; } = "MXN";
        public int NumParcialidad { get; set; }
        public decimal ImpSaldoAnt { get; set; }
        public decimal ImpPagado { get; set; }
        public decimal ImpSaldoInsoluto { get; set; }
        public decimal? IvaBase { get; set; }
        public decimal? IvaTasa { get; set; }
        public decimal? IvaImporte { get; set; }

        public string FolioTitulo => string.IsNullOrWhiteSpace(Serie) ? $"{Folio}" : $"{Serie}{Folio}";
        public string FechaPagoTexto => FechaPago.ToString("dd/MM/yyyy");
        public string ImpPagadoTexto => ImpPagado.ToString("C2", new CultureInfo("es-MX"));
        public string FormaPagoTexto => SatFormaPagoCatalogo.Describir(FormaPago);
        public string DescripcionTexto => $"Complemento {FolioTitulo} · Parcialidad {NumParcialidad} · {ImpPagadoTexto} · {FechaPagoTexto}";
    }

    /// <summary>Detalle completo de un Complemento de Pago, para armar su PDF.</summary>
    public class ComplementoPagoDetalleDto
    {
        public FacturaResumenDto? Factura { get; set; }
        public List<ComplementoPagoPagoDto> Pagos { get; set; } = new();
    }

    public class ComplementoPagoPagoDto
    {
        public int IdPago { get; set; }
        public int Orden { get; set; }
        public DateTime FechaPago { get; set; }
        public string? FormaPago { get; set; }
        public string Moneda { get; set; } = "MXN";
        public decimal TipoCambio { get; set; } = 1m;
        public string? NumOperacion { get; set; }
        public List<ComplementoPagoDoctoDetalleDto> Doctos { get; set; } = new();

        public string FormaPagoTexto => SatFormaPagoCatalogo.Describir(FormaPago);
    }

    public class ComplementoPagoDoctoDetalleDto
    {
        public int IdDocto { get; set; }
        public int? IdAbonoFactura { get; set; }
        public int? IdFacturaPagada { get; set; }
        public string? FacturaPagadaSerie { get; set; }
        public string? FacturaPagadaFolio { get; set; }
        public string UuidDoctoRelacionado { get; set; } = string.Empty;
        public string? FolioDoctoRelacionado { get; set; }
        public int NumParcialidad { get; set; }
        public decimal ImpSaldoAnt { get; set; }
        public decimal ImpPagado { get; set; }
        public decimal ImpSaldoInsoluto { get; set; }
        public string ObjetoImp { get; set; } = "02";
        public decimal? IvaBase { get; set; }
        public decimal? IvaTasa { get; set; }
        public decimal? IvaImporte { get; set; }

        public string FacturaPagadaTexto => IdFacturaPagada.HasValue
            ? (string.IsNullOrWhiteSpace(FacturaPagadaSerie) ? $"Folio {FacturaPagadaFolio}" : $"{FacturaPagadaSerie}{FacturaPagadaFolio}")
            : $"Factura no localizada — UUID: {UuidDoctoRelacionado}";
    }

    /// <summary>
    /// Un docto de Complemento de Pago que ya cita una factura propia (id_factura_pagada) pero
    /// todavía no tiene ningún abono interno registrado -- candidato a ligar a un movimiento
    /// bancario desde Conciliación (botón "Complementos").
    /// </summary>
    public class ComplementoPagoPendienteMovimientoDto
    {
        public int IdDocto { get; set; }
        public int IdFacturaComplemento { get; set; }
        public string? ComplementoSerie { get; set; }
        public string? ComplementoFolio { get; set; }
        public int IdFacturaPagada { get; set; }
        public string? FacturaPagadaSerie { get; set; }
        public string? FacturaPagadaFolio { get; set; }
        public DateTime FechaPago { get; set; }
        public string? FormaPago { get; set; }
        public string Moneda { get; set; } = "MXN";
        public int NumParcialidad { get; set; }
        public decimal ImpPagado { get; set; }

        public string ComplementoFolioTitulo => string.IsNullOrWhiteSpace(ComplementoSerie) ? $"{ComplementoFolio}" : $"{ComplementoSerie}{ComplementoFolio}";
        public string FacturaPagadaFolioTitulo => string.IsNullOrWhiteSpace(FacturaPagadaSerie) ? $"{FacturaPagadaFolio}" : $"{FacturaPagadaSerie}{FacturaPagadaFolio}";
        public string ImpPagadoTexto => ImpPagado.ToString("C2", new CultureInfo("es-MX"));
        public string FormaPagoTexto => SatFormaPagoCatalogo.Describir(FormaPago);
    }

    /// <summary>Solicitud para ligar un docto de Complemento de Pago a un movimiento bancario.</summary>
    public class VincularComplementoMovimientoRequestDto
    {
        public int IdDocto { get; set; }
        public int IdMovimiento { get; set; }
    }

    /// <summary>
    /// Resultado de "Consolidar Complementos": parsea el XML de las facturas tipo Complemento de
    /// Pago que llegaron sin pasar por el flujo de generación en la app (timbradas a mano en el
    /// portal de Bilkon, o traídas por una recarga de FEL) y las vincula a la factura que pagan.
    /// </summary>
    public class ComplementoPagoConsolidarResultDto
    {
        public int FacturasEncontradas { get; set; }
        public int FacturasParseadas { get; set; }
        public int PagosProcesados { get; set; }
        public int DoctosProcesados { get; set; }
        public List<string> Errores { get; set; } = new();
    }
}
