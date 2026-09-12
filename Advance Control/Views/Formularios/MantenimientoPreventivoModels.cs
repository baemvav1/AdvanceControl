using System;

namespace Advance_Control.Views.Formularios
{
    /// <summary>
    /// Fila de una tabla de mantenimiento preventivo (Cabina, Cuarto de máquinas, Foso, Pasillo, Techo de cabina).
    /// </summary>
    public class ChecklistItem
    {
        /// <summary>
        /// Identificador único de la fila, usado como GroupName de los RadioButton
        /// de la fila para que la selección (No aplica/Verificación/Ajuste/Limpieza/
        /// Lubricación/Recorrido) sea exclusiva por renglón sin chocar con otras filas.
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString();

        public string Texto { get; set; } = string.Empty;
        public bool NoAplica { get; set; }
        public bool Verificacion { get; set; }
        public bool Ajuste { get; set; }
        public bool Limpieza { get; set; }
        public bool Lubricacion { get; set; }
        public bool Recorrido { get; set; }
    }
}
