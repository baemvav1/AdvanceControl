using System;
using System.Text;

namespace Advance_Control.Utilities
{
    /// <summary>
    /// Convierte números enteros y montos monetarios a su representación en letras
    /// en español, usada para el texto legal de los contratos de suscripción
    /// (p.ej. "$ 46,906.99 (CUARENTA Y SEIS MIL NOVECIENTOS SEIS PESOS 99/100) MN").
    /// </summary>
    public static class NumeroALetrasHelper
    {
        private static readonly string[] Unidades =
        {
            "CERO", "UNO", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE"
        };

        private static readonly string[] Especiales10a19 =
        {
            "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE",
            "DIECISÉIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE"
        };

        private static readonly string[] Decenas =
        {
            "", "", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA"
        };

        private static readonly string[] VeintiUnidades =
        {
            "VEINTE", "VEINTIUNO", "VEINTIDÓS", "VEINTITRÉS", "VEINTICUATRO",
            "VEINTICINCO", "VEINTISÉIS", "VEINTISIETE", "VEINTIOCHO", "VEINTINUEVE"
        };

        private static readonly string[] Centenas =
        {
            "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS",
            "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS"
        };

        /// <summary>
        /// Convierte un entero no negativo a letras en español, en mayúsculas.
        /// Soporta hasta 999,999,999.
        /// </summary>
        public static string ConvertirEnteroALetras(long numero)
        {
            if (numero == 0) return "CERO";
            if (numero < 0) return $"MENOS {ConvertirEnteroALetras(-numero)}";

            var sb = new StringBuilder();

            var millones = numero / 1_000_000;
            var resto = numero % 1_000_000;
            var miles = resto / 1_000;
            var centenas = resto % 1_000;

            if (millones > 0)
            {
                sb.Append(millones == 1 ? "UN MILLÓN" : $"{ConvertirGrupo((int)millones)} MILLONES");
                if (miles > 0 || centenas > 0) sb.Append(' ');
            }

            if (miles > 0)
            {
                sb.Append(miles == 1 ? "MIL" : $"{ConvertirGrupo((int)miles)} MIL");
                if (centenas > 0) sb.Append(' ');
            }

            if (centenas > 0 || sb.Length == 0)
            {
                sb.Append(ConvertirGrupo((int)centenas));
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Convierte un grupo de 0-999 a letras
        /// </summary>
        private static string ConvertirGrupo(int n)
        {
            if (n == 0) return "";
            if (n == 100) return "CIEN";

            var sb = new StringBuilder();

            var c = n / 100;
            var resto = n % 100;

            if (c > 0)
            {
                sb.Append(Centenas[c]);
                if (resto > 0) sb.Append(' ');
            }

            if (resto > 0)
            {
                sb.Append(ConvertirDecenaUnidad(resto));
            }

            return sb.ToString();
        }

        private static string ConvertirDecenaUnidad(int n)
        {
            if (n < 10) return Unidades[n];
            if (n < 20) return Especiales10a19[n - 10];
            if (n < 30) return VeintiUnidades[n - 20];

            var d = n / 10;
            var u = n % 10;

            return u == 0 ? Decenas[d] : $"{Decenas[d]} Y {Unidades[u]}";
        }

        /// <summary>
        /// Convierte un monto monetario a su representación legal en letras,
        /// p.ej. 46906.99 -&gt; "CUARENTA Y SEIS MIL NOVECIENTOS SEIS PESOS 99/100 M.N."
        /// </summary>
        public static string ConvertirMontoALetras(decimal monto)
        {
            var entero = (long)Math.Truncate(monto);
            var centavos = (int)Math.Round((monto - entero) * 100, MidpointRounding.AwayFromZero);

            var letrasEntero = ConvertirEnteroALetras(entero);
            var etiquetaPesos = entero == 1 ? "PESO" : "PESOS";

            return $"{letrasEntero} {etiquetaPesos} {centavos:00}/100 M.N.";
        }

        /// <summary>
        /// Convierte un número de unidades pequeño a letras (p.ej. para "3 (TRES) unidad(es)")
        /// </summary>
        public static string ConvertirUnidadesALetras(int numero) => ConvertirEnteroALetras(numero);
    }
}
