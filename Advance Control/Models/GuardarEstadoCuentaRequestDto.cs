using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    public class GuardarEstadoCuentaRequestDto
    {
        [JsonPropertyName("versionXml")]
        public string VersionXml { get; set; } = "2.0";

        [JsonPropertyName("numeroCuenta")]
        public string NumeroCuenta { get; set; } = string.Empty;

        [JsonPropertyName("clabe")]
        public string Clabe { get; set; } = string.Empty;

        [JsonPropertyName("tipoCuenta")]
        public string? TipoCuenta { get; set; }

        [JsonPropertyName("tipoMoneda")]
        public string? TipoMoneda { get; set; }

        [JsonPropertyName("fechaInicio")]
        public DateTime FechaInicio { get; set; }

        [JsonPropertyName("fechaFin")]
        public DateTime FechaFin { get; set; }

        [JsonPropertyName("fechaCorte")]
        public DateTime FechaCorte { get; set; }

        [JsonPropertyName("saldoInicial")]
        public decimal SaldoInicial { get; set; }

        [JsonPropertyName("totalCargos")]
        public decimal TotalCargos { get; set; }

        [JsonPropertyName("totalAbonos")]
        public decimal TotalAbonos { get; set; }

        [JsonPropertyName("saldoFinal")]
        public decimal SaldoFinal { get; set; }

        [JsonPropertyName("totalComisiones")]
        public decimal TotalComisiones { get; set; }

        [JsonPropertyName("totalISR")]
        public decimal TotalISR { get; set; }

        [JsonPropertyName("totalIVA")]
        public decimal TotalIVA { get; set; }

        [JsonPropertyName("totalTransaccionesIndividuales")]
        public int TotalTransaccionesIndividuales { get; set; }

        [JsonPropertyName("totalGrupos")]
        public int TotalGrupos { get; set; }

        [JsonPropertyName("nombreBanco")]
        public string? NombreBanco { get; set; }

        [JsonPropertyName("rfcBanco")]
        public string? RfcBanco { get; set; }

        [JsonPropertyName("nombreSucursal")]
        public string? NombreSucursal { get; set; }

        [JsonPropertyName("direccionSucursal")]
        public string? DireccionSucursal { get; set; }

        [JsonPropertyName("titular")]
        public string? Titular { get; set; }

        [JsonPropertyName("rfcTitular")]
        public string? RfcTitular { get; set; }

        [JsonPropertyName("numeroCliente")]
        public string? NumeroCliente { get; set; }

        [JsonPropertyName("direccionTitular")]
        public string? DireccionTitular { get; set; }

        [JsonPropertyName("folioFiscal")]
        public string? FolioFiscal { get; set; }

        [JsonPropertyName("certificadoEmisor")]
        public string? CertificadoEmisor { get; set; }

        [JsonPropertyName("fechaEmisionCert")]
        public string? FechaEmisionCert { get; set; }

        [JsonPropertyName("certificadoSat")]
        public string? CertificadoSat { get; set; }

        [JsonPropertyName("fechaCertificacionSat")]
        public string? FechaCertificacionSat { get; set; }

        [JsonPropertyName("regimenFiscal")]
        public string? RegimenFiscal { get; set; }

        [JsonPropertyName("metodoPago")]
        public string? MetodoPago { get; set; }

        [JsonPropertyName("formaPago")]
        public string? FormaPago { get; set; }

        [JsonPropertyName("usoCfdi")]
        public string? UsoCfdi { get; set; }

        [JsonPropertyName("claveProdServ")]
        public string? ClaveProdServ { get; set; }

        [JsonPropertyName("lugarExpedicion")]
        public string? LugarExpedicion { get; set; }

        [JsonPropertyName("grupos")]
        public List<GuardarEstadoCuentaGrupoDto> Grupos { get; set; } = new();
    }
}
