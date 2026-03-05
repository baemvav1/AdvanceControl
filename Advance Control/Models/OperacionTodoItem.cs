using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Advance_Control.Models
{
    /// <summary>
    /// Item del ToDo de operaciones en el Dashboard.
    /// Representa una operación activa asignada al usuario con sus pasos pendientes/completados.
    /// </summary>
    public class OperacionTodoItem : INotifyPropertyChanged
    {
        private bool _expand;

        public int IdOperacion { get; set; }
        public string Nota { get; set; } = string.Empty;
        public string RazonSocial { get; set; } = string.Empty;
        public List<CheckPasoItem> Pasos { get; set; } = new();

        /// <summary>Indica si el card está expandido.</summary>
        public bool Expand
        {
            get => _expand;
            set
            {
                if (_expand != value)
                {
                    _expand = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ExpandGlyph));
                }
            }
        }

        /// <summary>Glifo del botón expandir/colapsar.</summary>
        public string ExpandGlyph => _expand ? "\uE70E" : "\uE70D"; // ChevronUp / ChevronDown

        /// <summary>Pasos completados.</summary>
        public int PasosCompletados => Pasos.Count(p => p.Completado);

        /// <summary>Total de pasos.</summary>
        public int TotalPasos => Pasos.Count;

        /// <summary>Resumen de progreso: "3 / 8"</summary>
        public string Progreso => $"{PasosCompletados} / {TotalPasos}";

        /// <summary>True cuando al menos un paso está pendiente.</summary>
        public bool TienePendientes => Pasos.Any(p => !p.Completado);

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Paso individual de una operación en el ToDo del Dashboard.
    /// </summary>
    public class CheckPasoItem
    {
        public string Nombre { get; set; } = string.Empty;
        public bool Completado { get; set; }

        /// <summary>Glifo de estado: ✓ verde / ○ gris</summary>
        public string Glyph => Completado ? "\uE73E" : "\uEA3A"; // Accept / CircleRing
    }
}
