using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Advance_Control.Utilities
{
    /// <summary>
    /// Geometría de áreas: GeoJSON de estados/municipios embebido en el ensamblado, contorno de un
    /// estado/municipio (anillo exterior del polígono más grande + bbox, igual que AreasPage y la
    /// migración 150) y punto en polígono (misma regla que fn_punto_en_poligono).
    /// </summary>
    public static class GeoContornos
    {
        private static byte[]? _estados;
        private static byte[]? _municipios;

        public static byte[]? Estados => _estados ??= Leer("Advance_Control.Geo.estados.json");
        public static byte[]? Municipios => _municipios ??= Leer("Advance_Control.Geo.municipios.json");

        private static byte[]? Leer(string recurso)
        {
            try
            {
                using var stream = typeof(GeoContornos).Assembly.GetManifestResourceStream(recurso);
                if (stream is null) return null;
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }

        public sealed class Contorno
        {
            public List<(decimal Lat, decimal Lng)> Path { get; init; } = new();
            public decimal Norte { get; init; }
            public decimal Sur { get; init; }
            public decimal Este { get; init; }
            public decimal Oeste { get; init; }
        }

        /// <summary>Contorno de un estado ("Estado") o municipio ("Municipio"); null si no se encuentra.</summary>
        public static Contorno? Obtener(string geoType, string nombre, string? estado)
        {
            var bytes = geoType == "Estado" ? Estados : Municipios;
            if (bytes is null) return null;

            using var doc = JsonDocument.Parse(bytes);
            foreach (var feature in doc.RootElement.GetProperty("features").EnumerateArray())
            {
                var props = feature.GetProperty("properties");
                if (!props.TryGetProperty("nombre", out var pNombre) || pNombre.GetString() != nombre) continue;
                if (geoType == "Municipio" && estado is not null
                    && (!props.TryGetProperty("estado", out var pEstado) || pEstado.GetString() != estado)) continue;

                var geometry = feature.GetProperty("geometry");
                var coords = geometry.GetProperty("coordinates");
                var poligonos = geometry.GetProperty("type").GetString() == "MultiPolygon"
                    ? coords.EnumerateArray().ToList()
                    : new List<JsonElement> { coords };

                List<(decimal Lat, decimal Lng)>? mayor = null;
                double mayorArea = -1;
                decimal norte = decimal.MinValue, sur = decimal.MaxValue, este = decimal.MinValue, oeste = decimal.MaxValue;

                foreach (var poligono in poligonos)
                {
                    var anillo = poligono.EnumerateArray().First().EnumerateArray()
                        .Select(p => (Lat: Math.Round(p[1].GetDecimal(), 5), Lng: Math.Round(p[0].GetDecimal(), 5)))
                        .ToList();
                    if (anillo.Count < 3) continue;

                    foreach (var (lat, lng) in anillo)
                    {
                        norte = Math.Max(norte, lat); sur = Math.Min(sur, lat);
                        este = Math.Max(este, lng); oeste = Math.Min(oeste, lng);
                    }

                    double area = 0;
                    for (int i = 0, j = anillo.Count - 1; i < anillo.Count; j = i++)
                        area += (double)(anillo[j].Lng * anillo[i].Lat - anillo[i].Lng * anillo[j].Lat);
                    area = Math.Abs(area);
                    if (area > mayorArea) { mayorArea = area; mayor = anillo; }
                }

                return mayor is null ? null : new Contorno { Path = mayor, Norte = norte, Sur = sur, Este = este, Oeste = oeste };
            }
            return null;
        }

        /// <summary>Lee el "path" ([{lat,lng},…]) de metadata_json de un área; vacío si no tiene.</summary>
        public static List<(double Lat, double Lng)> PathDeMetadata(string? metadataJson)
        {
            var path = new List<(double, double)>();
            if (string.IsNullOrWhiteSpace(metadataJson)) return path;
            try
            {
                using var doc = JsonDocument.Parse(metadataJson);
                if (doc.RootElement.TryGetProperty("path", out var p) && p.ValueKind == JsonValueKind.Array)
                    foreach (var v in p.EnumerateArray())
                        if (v.TryGetProperty("lat", out var la) && v.TryGetProperty("lng", out var ln))
                            path.Add((la.GetDouble(), ln.GetDouble()));
            }
            catch (JsonException) { }
            return path;
        }

        /// <summary>Ray casting, igual que fn_punto_en_poligono.</summary>
        public static bool PuntoEnPoligono(double lat, double lng, IReadOnlyList<(double Lat, double Lng)> path)
        {
            if (path.Count < 3) return false;
            bool dentro = false;
            for (int i = 0, j = path.Count - 1; i < path.Count; j = i++)
            {
                var (yi, xi) = path[i];
                var (yj, xj) = path[j];
                if ((yi > lat) != (yj > lat) && lng < (xj - xi) * (lat - yi) / (yj - yi) + xi)
                    dentro = !dentro;
            }
            return dentro;
        }
    }
}
