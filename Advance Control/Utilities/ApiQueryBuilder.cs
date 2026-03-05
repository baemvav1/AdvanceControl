using System;
using System.Collections.Generic;
using System.Globalization;

namespace Advance_Control.Utilities
{
    /// <summary>
    /// Construye query strings de URL de forma segura y con cultura invariante para valores numéricos.
    /// Elimina el patrón repetido de List&lt;string&gt; + string.Join en todos los servicios HTTP.
    /// Los valores decimales/float se serializan siempre con punto como separador (InvariantCulture),
    /// lo que evita que el sistema operativo envíe comas cuando el idioma del SO es español.
    /// </summary>
    public sealed class ApiQueryBuilder
    {
        private readonly List<string> _params = new();

        // ── Cadenas ─────────────────────────────────────────────────────────────
        public ApiQueryBuilder Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                _params.Add($"{key}={Uri.EscapeDataString(value)}");
            return this;
        }

        // ── Enteros ──────────────────────────────────────────────────────────────
        public ApiQueryBuilder Add(string key, int? value)
        {
            if (value.HasValue)
                _params.Add($"{key}={value.Value}");
            return this;
        }

        public ApiQueryBuilder Add(string key, long? value)
        {
            if (value.HasValue)
                _params.Add($"{key}={value.Value}");
            return this;
        }

        /// <summary>Agrega el parámetro sólo si el valor es mayor que cero.</summary>
        public ApiQueryBuilder AddPositive(string key, int value)
        {
            if (value > 0)
                _params.Add($"{key}={value}");
            return this;
        }

        // ── Numéricos con decimales (siempre InvariantCulture) ────────────────
        public ApiQueryBuilder Add(string key, double? value)
        {
            if (value.HasValue)
                _params.Add($"{key}={value.Value.ToString("G", CultureInfo.InvariantCulture)}");
            return this;
        }

        public ApiQueryBuilder Add(string key, float? value)
        {
            if (value.HasValue)
                _params.Add($"{key}={value.Value.ToString("G", CultureInfo.InvariantCulture)}");
            return this;
        }

        public ApiQueryBuilder Add(string key, decimal? value)
        {
            if (value.HasValue)
                _params.Add($"{key}={value.Value.ToString("G", CultureInfo.InvariantCulture)}");
            return this;
        }

        /// <summary>Versión directa (no nullable) para campos requeridos.</summary>
        public ApiQueryBuilder AddRequired(string key, double value)
        {
            _params.Add($"{key}={value.ToString("G", CultureInfo.InvariantCulture)}");
            return this;
        }

        public ApiQueryBuilder AddRequired(string key, decimal value)
        {
            _params.Add($"{key}={value.ToString("G", CultureInfo.InvariantCulture)}");
            return this;
        }

        public ApiQueryBuilder AddRequired(string key, int value)
        {
            _params.Add($"{key}={value}");
            return this;
        }

        // ── Booleanos ─────────────────────────────────────────────────────────
        public ApiQueryBuilder Add(string key, bool? value)
        {
            if (value.HasValue)
                _params.Add($"{key}={value.Value.ToString().ToLowerInvariant()}");
            return this;
        }

        public ApiQueryBuilder AddRequired(string key, bool value)
        {
            _params.Add($"{key}={value.ToString().ToLowerInvariant()}");
            return this;
        }

        // ── Construcción final ────────────────────────────────────────────────
        /// <summary>
        /// Devuelve la URL base con los parámetros concatenados.
        /// Si no hay parámetros, devuelve la URL base sin cambios.
        /// </summary>
        public string Build(string baseUrl)
            => _params.Count > 0 ? $"{baseUrl}?{string.Join("&", _params)}" : baseUrl;

        /// <summary>Conveniencia estática para casos de un solo uso.</summary>
        public static ApiQueryBuilder For(string baseUrl) => new ApiQueryBuilder();
    }
}
