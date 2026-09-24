using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Utilities
{
    /// <summary>
    /// Conecta un AutoSuggestBox usado como filtro de catálogo: al escribir, sugiere
    /// valores existentes del catálogo que contengan el texto tecleado; al elegir una
    /// sugerencia o presionar Enter, dispara la búsqueda.
    /// </summary>
    public static class AutoSuggestHelper
    {
        public static List<string> Sugerir(IEnumerable<string?> fuente, string? texto, int max = 8)
        {
            IEnumerable<string> candidatos = fuente
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(texto))
                candidatos = candidatos.Where(v => v.Contains(texto, StringComparison.OrdinalIgnoreCase));

            return candidatos.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).Take(max).ToList();
        }

        /// <summary>
        /// Cablea un AutoSuggestBox de filtro. <paramref name="fuente"/> se evalúa en cada
        /// tecleo (para reflejar el catálogo más reciente), y <paramref name="buscar"/> se
        /// invoca al elegir una sugerencia o al presionar Enter/lupa.
        /// </summary>
        public static void Conectar(AutoSuggestBox box, Func<IEnumerable<string?>> fuente, Action buscar)
        {
            box.TextChanged += (s, e) =>
            {
                if (e.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                    s.ItemsSource = Sugerir(fuente(), s.Text);
            };
            box.QuerySubmitted += (s, e) => buscar();
            box.SuggestionChosen += (s, e) =>
            {
                if (e.SelectedItem is string texto)
                    s.Text = texto;
            };
        }
    }
}
