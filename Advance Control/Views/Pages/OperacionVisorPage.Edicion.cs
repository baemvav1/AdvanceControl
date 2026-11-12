using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Activity;
using Advance_Control.Services.Notificacion;
using Advance_Control.Utilities;
using Microsoft.UI.Xaml.Controls;
using WinUI.TableView;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Edición en la tabla de cargos: doble clic sobre Cant., Unitario o Nota. Enter guarda,
    /// Esc cancela y salir de la celda también guarda. Con la cotización cerrada o la operación
    /// finalizada avisa y no entra a edición. Si el valor es inválido o el guardado falla, la
    /// celda regresa al valor anterior. Al guardar no hay aviso: el monto se recalcula, los PDF
    /// quedan viejos y se actualiza el total de la operación (igual que el visor).
    /// </summary>
    public sealed partial class OperacionVisorPage
    {
        private static readonly CultureInfo CulturaEdicion = new("es-MX");

        private void TablaCargos_BeginningEdit(object sender, TableViewBeginningEditEventArgs e)
        {
            if (e.DataItem is not CargoFila || e.Column?.Tag is not string)
            {
                e.Cancel = true;
                return;
            }

            if (_operacion == null || _operacion.IsSharedReadOnly || _operacion.IsFinalized || _operacion.CotFinalizada)
            {
                e.Cancel = true;
                _ = PuedeEditarCargosAsync("editar cargos");
            }
        }

        /// <summary>La celda se edita con el número sin formato de moneda, todo seleccionado.</summary>
        private void TablaCargos_PreparingCellForEdit(object sender, TableViewPreparingCellForEditEventArgs e)
        {
            if (e.DataItem is CargoFila fila && e.Column?.Tag is string campo && e.EditingElement is TextBox caja)
            {
                caja.Text = fila.TextoParaEditar(campo);
                caja.SelectAll();
            }
        }

        private void TablaCargos_CellEditEnding(object sender, TableViewCellEditEndingEventArgs e)
        {
            if (e.EditAction != TableViewEditAction.Commit
                || e.DataItem is not CargoFila fila
                || e.Column?.Tag is not string campo
                || e.EditingElement is not TextBox caja)
            {
                return;
            }

            var texto = caja.Text?.Trim() ?? string.Empty;
            var cantidad = fila.Cantidad;
            var unitario = fila.Unitario;
            var nota = fila.Nota;
            string? error = null;

            switch (campo)
            {
                case nameof(CargoFila.Cantidad):
                    if (!LeerNumero(texto, out cantidad) || cantidad <= 0)
                        error = "La cantidad debe ser un número mayor que 0.";
                    else if (fila.EsServicio && cantidad % 1 != 0)
                        error = "En servicios la cantidad debe ser un número entero.";
                    break;
                case nameof(CargoFila.Unitario):
                    if (!LeerNumero(texto, out unitario) || unitario < 0)
                        error = "El unitario debe ser un número igual o mayor que 0.";
                    break;
                default:
                    nota = texto;
                    break;
            }

            // El binding es de una vía: si no se guarda, la celda vuelve sola al valor anterior.
            if (error != null)
            {
                _ = _notificaciones.MostrarAsync("Valor inválido", error);
                return;
            }

            if (cantidad == fila.Cantidad && unitario == fila.Unitario && nota == fila.Nota)
            {
                return;
            }

            _ = GuardarEdicionAsync(fila, cantidad, unitario, nota);
        }

        /// <summary>Acepta "1,234.50", "$1,234.50" o "1234.5".</summary>
        private static bool LeerNumero(string texto, out double valor) =>
            double.TryParse(texto, NumberStyles.Currency, CulturaEdicion, out valor)
            || double.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out valor);

        private async Task GuardarEdicionAsync(CargoFila fila, double cantidad, double unitario, string nota)
        {
            var (cantidadAntes, unitarioAntes, notaAntes) = (fila.Cantidad, fila.Unitario, fila.Nota);
            fila.Aplicar(cantidad, unitario, nota);

            try
            {
                var ok = await _cargos.UpdateCargoAsync(new CargoEditDto
                {
                    IdCargo = fila.IdCargo,
                    IdTipoCargo = fila.IdTipoCargo,
                    IdRelacionCargo = fila.IdRelacionCargo,
                    Cantidad = cantidad,
                    Unitario = unitario,
                    Monto = fila.Monto,
                    Nota = nota
                });
                if (!ok)
                {
                    fila.Aplicar(cantidadAntes, unitarioAntes, notaAntes);
                    await _notificaciones.MostrarAsync("Editar cargo", "No se pudo guardar el cambio del cargo.");
                    return;
                }
            }
            catch (Exception ex)
            {
                fila.Aplicar(cantidadAntes, unitarioAntes, notaAntes);
                await _notificaciones.MostrarAsync("Editar cargo", $"No se pudo guardar el cambio del cargo. {ex.Message}");
                return;
            }

            _actividad.Registrar("Operaciones", "Cargo modificado");

            // Cotización / reporte / nota leen los cargos de la operación.
            if (_operacion?.Cargos.FirstOrDefault(c => c.IdCargo == fila.IdCargo) is { } cargo)
            {
                cargo.Cantidad = cantidad;
                cargo.Unitario = unitario;
                cargo.Nota = nota;
            }

            var habiaPdfs = new[] { "Cotizacion", "Reporte", "Nota" }
                .Any(tipo => !string.IsNullOrEmpty(_operaciones.FindExistingPdf(_idOperacion, tipo)));
            await CargosCambiaronAsync();
            if (habiaPdfs)
            {
                // Los PDF generados se borraron: la galería deja de mostrarlos.
                await RefrescarGaleriaAsync();
            }
        }
    }
}
