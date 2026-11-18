using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Utilities
{
    /// <summary>
    /// Validación de correos con regex para los campos de captura (contactos, usuarios, envío).
    /// Evita direcciones como "x@dominio,com" que después truenan al enviar.
    /// </summary>
    public static class CorreoValidacion
    {
        /// <summary>usuario@dominio.ext: sin espacios ni comas, dominio con punto y extensión de 2+ letras.</summary>
        public static readonly Regex Patron = new(
            @"^[A-Za-z0-9._%+'\-]+@[A-Za-z0-9](?:[A-Za-z0-9\-]*[A-Za-z0-9])?(?:\.[A-Za-z0-9](?:[A-Za-z0-9\-]*[A-Za-z0-9])?)*\.[A-Za-z]{2,}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static bool EsValido(string? correo)
            => !string.IsNullOrWhiteSpace(correo) && Patron.IsMatch(correo.Trim());

        /// <summary>Separa una lista "a@x.com, b@y.com; c@z.com".</summary>
        public static List<string> Separar(string? lista)
            => string.IsNullOrWhiteSpace(lista)
                ? new List<string>()
                : lista.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim())
                    .Where(c => c.Length > 0)
                    .ToList();

        /// <summary>Mensaje de error si el correo (opcional) no es válido; null si está vacío o bien.</summary>
        public static string? Error(string? correo, string campo = "correo")
        {
            if (string.IsNullOrWhiteSpace(correo) || EsValido(correo)) return null;
            return $"El {campo} \"{correo.Trim()}\" no es válido. Usa el formato nombre@dominio.com (sin espacios ni comas).";
        }

        /// <summary>Mensaje de error con el primer correo inválido de una lista; null si todos están bien.</summary>
        public static string? ErrorLista(string? lista, string campo)
        {
            var malo = Separar(lista).FirstOrDefault(c => !EsValido(c));
            return malo == null ? null : $"En {campo}, \"{malo}\" no es un correo válido. Usa el formato nombre@dominio.com y separa varios con coma.";
        }

        /// <summary>
        /// Marca el TextBox en rojo con una leyenda mientras el texto no sea un correo válido
        /// (o una lista válida si <paramref name="lista"/>). Vacío no se marca.
        /// </summary>
        public static void ValidarEnVivo(this TextBox caja, bool lista = false)
        {
            var bordeOriginal = caja.BorderBrush;
            void Revisar()
            {
                var error = lista ? ErrorLista(caja.Text, "este campo") : Error(caja.Text);
                if (error == null)
                {
                    caja.BorderBrush = bordeOriginal;
                    caja.ClearValue(TextBox.DescriptionProperty);
                }
                else
                {
                    caja.BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    caja.Description = lista ? "Algún correo no es válido (nombre@dominio.com, separados por coma)" : "Correo no válido (nombre@dominio.com)";
                }
            }

            caja.TextChanged += (_, _) => Revisar();
            Revisar();
        }
    }
}
