using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Advance_Control.Views.Formularios
{
    /// <summary>
    /// Fila de una tabla de mantenimiento preventivo (Cabina, Cuarto de máquinas, Foso, Pasillo, Techo de cabina).
    /// </summary>
    public class ChecklistItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Identificador único de la fila, usado como GroupName de los RadioButton
        /// de la fila para que la selección (No aplica/Verificación/Ajuste/Limpieza/
        /// Lubricación/Recorrido) sea exclusiva por renglón sin chocar con otras filas.
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString();

        public string Texto { get; set; } = string.Empty;

        private bool _noAplica;
        public bool NoAplica
        {
            get => _noAplica;
            set { if (_noAplica != value) { _noAplica = value; OnPropertyChanged(); } }
        }

        private bool _verificacion;
        public bool Verificacion
        {
            get => _verificacion;
            set { if (_verificacion != value) { _verificacion = value; OnPropertyChanged(); } }
        }

        private bool _ajuste;
        public bool Ajuste
        {
            get => _ajuste;
            set { if (_ajuste != value) { _ajuste = value; OnPropertyChanged(); } }
        }

        private bool _limpieza;
        public bool Limpieza
        {
            get => _limpieza;
            set { if (_limpieza != value) { _limpieza = value; OnPropertyChanged(); } }
        }

        private bool _lubricacion;
        public bool Lubricacion
        {
            get => _lubricacion;
            set { if (_lubricacion != value) { _lubricacion = value; OnPropertyChanged(); } }
        }

        private bool _recorrido;
        public bool Recorrido
        {
            get => _recorrido;
            set { if (_recorrido != value) { _recorrido = value; OnPropertyChanged(); } }
        }
    }
}
