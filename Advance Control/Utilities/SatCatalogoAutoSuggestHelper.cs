using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.Models;

namespace Advance_Control.Utilities
{
    /// <summary>Configura un AutoSuggestBox para elegir una clave de un catálogo SAT (Régimen Fiscal, Uso CFDI)
    /// filtrando localmente por clave o descripción conforme el usuario escribe.</summary>
    public static class SatCatalogoAutoSuggestHelper
    {
        public static void Configurar(AutoSuggestBox box, IReadOnlyList<SatCatalogoItemDto> catalogo, string? valorInicial = null)
        {
            box.ItemsSource = catalogo;

            box.TextChanged += (s, e) =>
            {
                if (e.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;

                var texto = box.Text?.Trim() ?? "";
                box.ItemsSource = string.IsNullOrEmpty(texto)
                    ? catalogo
                    : catalogo.Where(c =>
                        c.Clave.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                        c.Descripcion.Contains(texto, StringComparison.OrdinalIgnoreCase)).ToList();
            };

            box.SuggestionChosen += (s, e) =>
            {
                if (e.SelectedItem is SatCatalogoItemDto item) box.Text = item.ToString();
            };

            if (!string.IsNullOrWhiteSpace(valorInicial))
            {
                var coincidencia = catalogo.FirstOrDefault(c => c.Clave == valorInicial);
                box.Text = coincidencia?.ToString() ?? valorInicial;
            }
        }

        /// <summary>El texto del AutoSuggestBox puede ser "clave - descripción" (elegido de la lista) o una clave escrita a mano.</summary>
        public static string ExtraerClave(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            var separadorIndex = texto.IndexOf(" - ", StringComparison.Ordinal);
            return (separadorIndex > 0 ? texto[..separadorIndex] : texto).Trim();
        }
    }
}
