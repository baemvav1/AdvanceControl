using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Services.Entidades;

namespace Advance_Control.Models
{
    /// <summary>
    /// Opción del ComboBox "Entidad" de Facturas, Conciliación y Cobranza: una razón social (con
    /// su RFC y sus cuentas bancarias) o "Todas las entidades".
    /// </summary>
    public sealed class EntidadFiltroItem
    {
        public EntidadDto? Entidad { get; init; }
        public string Texto { get; init; } = string.Empty;

        /// <summary>Números de cuenta normalizados (ver EntidadCuentaBancariaDto.Normalizar).</summary>
        public HashSet<string> Cuentas { get; init; } = new();
        public HashSet<string> Clabes { get; init; } = new();

        public bool EsTodas => Entidad == null;
        public int? IdEntidad => Entidad?.IdEntidad;
        public string? Rfc => Entidad?.RFC?.Trim();

        public override string ToString() => Texto;

        /// <summary>La factura (por su RFC emisor) pertenece a esta entidad.</summary>
        public bool CoincideRfc(string? rfcEmisor)
            => EsTodas || string.Equals(Rfc, rfcEmisor?.Trim(), StringComparison.OrdinalIgnoreCase);

        /// <summary>El estado de cuenta (por número o CLABE) es de una cuenta de esta entidad.</summary>
        public bool CoincideCuenta(string? numeroCuenta, string? clabe)
        {
            if (EsTodas) return true;
            var numero = EntidadCuentaBancariaDto.Normalizar(numeroCuenta);
            if (numero.Length > 0 && Cuentas.Contains(numero)) return true;
            return !string.IsNullOrWhiteSpace(clabe) && Clabes.Contains(clabe.Trim());
        }

        /// <summary>
        /// Opciones del ComboBox. La predeterminada va primero; "Todas" al final si se pide.
        /// Regresa también la opción a seleccionar al abrir (la predeterminada).
        /// </summary>
        public static async Task<(List<EntidadFiltroItem> Opciones, EntidadFiltroItem? Inicial)> CargarAsync(IEntidadService entidadService, bool incluirTodas)
        {
            var entidades = await entidadService.GetEntidadesAsync(new EntidadQueryDto { Estatus = true }) ?? new List<EntidadDto>();
            List<EntidadCuentaBancariaDto> cuentas;
            try
            {
                cuentas = await entidadService.GetCuentasBancariasAsync();
            }
            catch
            {
                cuentas = new List<EntidadCuentaBancariaDto>();
            }

            var opciones = entidades
                .OrderByDescending(e => e.EsPredeterminada)
                .ThenBy(e => e.IdEntidad)
                .Select(e => new EntidadFiltroItem
                {
                    Entidad = e,
                    Texto = e.Descripcion,
                    Cuentas = cuentas.Where(c => c.IdEntidad == e.IdEntidad)
                        .Select(c => EntidadCuentaBancariaDto.Normalizar(c.NumeroCuenta))
                        .Where(n => n.Length > 0)
                        .ToHashSet(),
                    Clabes = cuentas.Where(c => c.IdEntidad == e.IdEntidad && !string.IsNullOrWhiteSpace(c.Clabe))
                        .Select(c => c.Clabe!.Trim())
                        .ToHashSet()
                })
                .ToList();

            var inicial = opciones.FirstOrDefault();
            if (incluirTodas && opciones.Count > 1)
                opciones.Add(new EntidadFiltroItem { Texto = "Todas las entidades" });

            return (opciones, inicial);
        }
    }
}
