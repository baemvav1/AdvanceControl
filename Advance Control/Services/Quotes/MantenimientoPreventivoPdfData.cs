using System.Collections.Generic;
using Advance_Control.Views.Formularios;

namespace Advance_Control.Services.Quotes
{
    /// <summary>
    /// Snapshot de los datos capturados en <see cref="Advance_Control.Views.Formularios.MantenimientoPreventivoWindow"/>
    /// al presionar "Finalizar", listo para renderizar como PDF.
    /// </summary>
    public sealed class MantenimientoPreventivoPdfData
    {
        public int IdOperacion { get; set; }

        public string? Proyecto { get; set; }
        public string? Direccion { get; set; }
        public string? Ruta { get; set; }
        public string? NoEquipo { get; set; }
        public string? RefeEquipo { get; set; }
        public string? Fecha { get; set; }
        public string? HoraEntrada { get; set; }
        public string? HoraSalida { get; set; }

        /// <summary>"Dirigido a: Nombre — correo" o null si no se seleccionó contacto.</summary>
        public string? DirigidoA { get; set; }

        /// <summary>Las 5 tablas de checklist en el orden en que aparecen en el formulario.</summary>
        public List<MantenimientoPreventivoSeccion> Secciones { get; set; } = new();

        public string? Observaciones { get; set; }

        /// <summary>"Operativo" o "Detenido".</summary>
        public string? SituacionFinal { get; set; }

        public string? TecnicoNombre { get; set; }
        public int? IdAtiende { get; set; }

        public string? ClienteNombre { get; set; }
        public string? ClienteCorreo { get; set; }
    }

    /// <summary>Una sección de checklist (p.ej. "Cabina") con sus renglones.</summary>
    public sealed class MantenimientoPreventivoSeccion
    {
        public string Nombre { get; }
        public IReadOnlyList<ChecklistItem> Items { get; }

        public MantenimientoPreventivoSeccion(string nombre, IReadOnlyList<ChecklistItem> items)
        {
            Nombre = nombre;
            Items = items;
        }
    }
}
