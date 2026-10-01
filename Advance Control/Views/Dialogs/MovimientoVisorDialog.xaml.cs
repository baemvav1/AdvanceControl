using System;
using Advance_Control.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Dialogs;

/// <summary>
/// Visor modal (solo lectura) de un movimiento bancario pendiente: monto, datos del pago,
/// cuenta que lo recibió, metadatos y movimientos relacionados.
/// </summary>
public sealed partial class MovimientoVisorDialog : ContentDialog
{
    public ConciliacionMovimientoFila Fila { get; }

    public string Encabezado => $"{Fila.FechaTexto} · {Fila.Grupo.TipoTitulo}";

    public string EtiquetaRfcReferencia => string.Equals(Fila.Grupo.TipoOperacion, "DEPOSITO_SBC", StringComparison.OrdinalIgnoreCase)
        ? "Folio del cheque"
        : "RFC emisor";

    public Visibility TieneRelacionados => Fila.Grupo.MovimientosRelacionados.Count > 0
        ? Visibility.Visible
        : Visibility.Collapsed;

    public MovimientoVisorDialog(ConciliacionMovimientoFila fila, XamlRoot xamlRoot)
    {
        Fila = fila ?? throw new ArgumentNullException(nameof(fila));
        InitializeComponent();
        XamlRoot = xamlRoot;
    }

    public string ValorOGuion(string? valor) => string.IsNullOrWhiteSpace(valor) ? "—" : valor.Trim();
}
