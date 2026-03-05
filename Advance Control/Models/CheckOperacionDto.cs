using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// Representa el estado de completitud de los pasos clave de una operación.
    /// </summary>
    public class CheckOperacionDto
    {
        [JsonPropertyName("idOperacion")]
        public int IdOperacion { get; set; }

        [JsonPropertyName("cotizacionGenerada")]
        public bool CotizacionGenerada { get; set; }

        [JsonPropertyName("cotizacionEnviada")]
        public bool CotizacionEnviada { get; set; }

        [JsonPropertyName("reporteGenerado")]
        public bool ReporteGenerado { get; set; }

        [JsonPropertyName("reporteEnviado")]
        public bool ReporteEnviado { get; set; }

        [JsonPropertyName("prefacturaCargada")]
        public bool PrefacturaCargada { get; set; }

        [JsonPropertyName("hojaServicioCargada")]
        public bool HojaServicioCargada { get; set; }

        [JsonPropertyName("ordenCompraCargada")]
        public bool OrdenCompraCargada { get; set; }

        [JsonPropertyName("facturaCargada")]
        public bool FacturaCargada { get; set; }

        [JsonPropertyName("fechaActualizacion")]
        public DateTime FechaActualizacion { get; set; }

        // -----------------------------------------------------------------------
        // Helpers calculados (no se serializan)
        // -----------------------------------------------------------------------

        /// <summary>Número de pasos completados (0-8).</summary>
        public int StepsCompletados =>
            (CotizacionGenerada ? 1 : 0) +
            (CotizacionEnviada  ? 1 : 0) +
            (ReporteGenerado    ? 1 : 0) +
            (ReporteEnviado     ? 1 : 0) +
            (PrefacturaCargada  ? 1 : 0) +
            (HojaServicioCargada? 1 : 0) +
            (OrdenCompraCargada ? 1 : 0) +
            (FacturaCargada     ? 1 : 0);

        /// <summary>Total de pasos disponibles.</summary>
        public int TotalSteps => 8;

        /// <summary>Porcentaje de pasos completados (0-100).</summary>
        public double PorcentajeCompletado => (StepsCompletados / (double)TotalSteps) * 100.0;

        /// <summary>True cuando todos los pasos están completados.</summary>
        public bool Completo => StepsCompletados == TotalSteps;

        /// <summary>Lista de pasos con su estado, para binding en XAML.</summary>
        public List<CheckPasoItem> Pasos => new()
        {
            new() { Nombre = "Cotización generada",    Completado = CotizacionGenerada  },
            new() { Nombre = "Cotización enviada",     Completado = CotizacionEnviada   },
            new() { Nombre = "Reporte generado",       Completado = ReporteGenerado     },
            new() { Nombre = "Reporte enviado",        Completado = ReporteEnviado      },
            new() { Nombre = "Prefactura cargada",     Completado = PrefacturaCargada   },
            new() { Nombre = "Hoja de servicio",       Completado = HojaServicioCargada },
            new() { Nombre = "Orden de compra",        Completado = OrdenCompraCargada  },
            new() { Nombre = "Factura cargada",        Completado = FacturaCargada      },
        };
    }
}
