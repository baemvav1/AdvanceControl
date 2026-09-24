using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Advance_Control.Models;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// Envoltorio seleccionable de un equipo, usado en el checklist de
    /// "Características técnicas de los equipos" del contrato.
    /// </summary>
    public class EquipoSeleccionableDto : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public EquipoDto Equipo { get; }

        public string EtiquetaDisplay => $"{Equipo.Identificador} — {Equipo.Marca}";

        /// <summary>
        /// Características técnicas que se plasman en la tabla del contrato
        /// (ver ContratoPdfService.GenerarContratoPdfAsync), para que el usuario
        /// vea de qué equipo se trata antes de incluirlo en la suscripción.
        /// </summary>
        public string DetalleTecnico
        {
            get
            {
                var partes = new List<string>();

                if (Equipo.Paradas.HasValue)
                    partes.Add($"Par/Des: {Equipo.Paradas}");
                if (!string.IsNullOrWhiteSpace(Equipo.Controlador))
                    partes.Add($"Controlador: {Equipo.Controlador}");
                if (!string.IsNullOrWhiteSpace(Equipo.TipoPuerta))
                    partes.Add($"Tipo Puerta: {Equipo.TipoPuerta}");
                if (!string.IsNullOrWhiteSpace(Equipo.Velocidad))
                    partes.Add($"Velocidad: {Equipo.Velocidad}");
                if (!string.IsNullOrWhiteSpace(Equipo.TipoMaquina))
                    partes.Add($"Tipo Máquina: {Equipo.TipoMaquina}");
                if (!string.IsNullOrWhiteSpace(Equipo.Operador))
                    partes.Add($"Operador: {Equipo.Operador}");

                return partes.Count > 0
                    ? string.Join("  ·  ", partes)
                    : "Sin datos técnicos capturados";
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        public EquipoSeleccionableDto(EquipoDto equipo)
        {
            Equipo = equipo;
        }
    }

    /// <summary>
    /// ViewModel para el formulario de generación de contrato de suscripción.
    /// </summary>
    public class GenerarContratoViewModel : ViewModelBase
    {
        private string _numeroContrato = string.Empty;
        private string _direccionInstalacion = string.Empty;
        private string _montoMensualText = string.Empty;
        private string _numeroUnidadesText = "0";
        private DateTimeOffset _vigenciaInicio = DateTimeOffset.Now;
        private DateTimeOffset _vigenciaFin = DateTimeOffset.Now.AddMonths(12);
        private string _nombreFirmante = string.Empty;
        private string _telefonoFirmante = string.Empty;
        private DateTimeOffset _fechaFirma = DateTimeOffset.Now;
        private string _errorMessage = string.Empty;
        private string _filtroEquipos = string.Empty;

        public string Nivel { get; }

        public GenerarContratoViewModel(string nivel)
        {
            Nivel = nivel;
        }

        public string NumeroContrato
        {
            get => _numeroContrato;
            set => SetProperty(ref _numeroContrato, value);
        }

        public string DireccionInstalacion
        {
            get => _direccionInstalacion;
            set => SetProperty(ref _direccionInstalacion, value);
        }

        public string MontoMensualText
        {
            get => _montoMensualText;
            set
            {
                if (SetProperty(ref _montoMensualText, value))
                    OnPropertyChanged(nameof(CanFinalizar));
            }
        }

        public decimal? MontoMensual =>
            decimal.TryParse(MontoMensualText, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var m) && m > 0
                ? m
                : null;

        public string NumeroUnidadesText
        {
            get => _numeroUnidadesText;
            set => SetProperty(ref _numeroUnidadesText, value);
        }

        public int NumeroUnidades => int.TryParse(NumeroUnidadesText, out var n) && n > 0 ? n : 0;

        public DateTimeOffset VigenciaInicio
        {
            get => _vigenciaInicio;
            set
            {
                if (SetProperty(ref _vigenciaInicio, value))
                {
                    // Por defecto, ajustar vigencia fin a 12 meses después (el usuario puede editarla después)
                    VigenciaFin = value.AddMonths(12);
                }
            }
        }

        public DateTimeOffset VigenciaFin
        {
            get => _vigenciaFin;
            set => SetProperty(ref _vigenciaFin, value);
        }

        public string NombreFirmante
        {
            get => _nombreFirmante;
            set => SetProperty(ref _nombreFirmante, value);
        }

        public string TelefonoFirmante
        {
            get => _telefonoFirmante;
            set => SetProperty(ref _telefonoFirmante, value);
        }

        public DateTimeOffset FechaFirma
        {
            get => _fechaFirma;
            set => SetProperty(ref _fechaFirma, value);
        }

        public ObservableCollection<EquipoSeleccionableDto> Equipos { get; } = new();

        public string FiltroEquipos
        {
            get => _filtroEquipos;
            set => SetProperty(ref _filtroEquipos, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                    OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public bool CanFinalizar =>
            MontoMensual.HasValue && NumeroUnidades > 0 && Equipos.Any(e => e.IsSelected);

        /// <summary>
        /// Recalcula NumeroUnidadesText a partir de los equipos actualmente seleccionados
        /// </summary>
        public void RecalcularNumeroUnidades()
        {
            NumeroUnidadesText = Equipos.Count(e => e.IsSelected).ToString();
            OnPropertyChanged(nameof(CanFinalizar));
        }
    }
}
