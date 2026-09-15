using System;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para los parámetros de búsqueda de operaciones
    /// </summary>
    public class OperacionQueryDto
    {
        /// <summary>
        /// Filtro exacto por idOperacion (0 para no filtrar)
        /// </summary>
        public int IdOperacion { get; set; }

        /// <summary>
        /// Filtro exacto por idTipo (0 para no filtrar)
        /// </summary>
        public int IdTipo { get; set; }

        /// <summary>
        /// Filtro exacto por idCliente (0 para no filtrar)
        /// </summary>
        public int IdCliente { get; set; }

        /// <summary>
        /// Filtro exacto por idEquipo (0 para no filtrar)
        /// </summary>
        public int IdEquipo { get; set; }

        /// <summary>
        /// Filtro exacto por idAtiende (0 para no filtrar)
        /// </summary>
        public int IdAtiende { get; set; }

        /// <summary>
        /// Búsqueda parcial en nota (permite búsqueda por texto)
        /// </summary>
        public string? Nota { get; set; }

        /// <summary>
        /// Fecha inicial del rango (fechaInicio >= FechaInicial). Nulo para no filtrar.
        /// </summary>
        public DateTimeOffset? FechaInicial { get; set; }

        /// <summary>
        /// Fecha final del rango (fechaFinal <= FechaFinalFiltro). Nulo para no filtrar.
        /// </summary>
        public DateTimeOffset? FechaFinalFiltro { get; set; }

        /// <summary>
        /// Si es true, incluye operaciones ya cerradas (fecha_final no nulo), que por defecto
        /// se ocultan del listado normal. Usado por el historial de cobranza.
        /// </summary>
        public bool IncluirFinalizadas { get; set; }

        /// <summary>
        /// Si es true, incluye operaciones que ya tienen una factura vinculada, que por
        /// defecto se ocultan del listado.
        /// </summary>
        public bool IncluirFacturadas { get; set; }

        /// <summary>
        /// Filtro de estado tipo OR (checkboxes T-Finalizado/Facturadas/Abiertas de
        /// OperacionesPage): si alguno de los 3 viene no-nulo, reemplaza por completo
        /// el filtro IncluirFinalizadas/IncluirFacturadas de arriba — una operación se
        /// muestra si cumple CUALQUIERA de los estados marcados en true (no son
        /// excluyentes: puede estar abierta y a la vez t-finalizada).
        /// </summary>
        public bool? EstadoAbierta { get; set; }
        public bool? EstadoTFinalizado { get; set; }
        public bool? EstadoFacturada { get; set; }

        /// <summary>
        /// 4to estado del filtro OR ("Abiertas con OC"): operaciones abiertas, sin
        /// t_finalizado y sin factura, que ya tienen orden de compra cargada.
        /// </summary>
        public bool? EstadoAbiertasConOc { get; set; }
    }
}
