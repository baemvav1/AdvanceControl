using System;

namespace Advance_Control.Models
{
    /// <summary>
    /// Modelo para ubicaciones de Google Maps
    /// </summary>
    public class UbicacionDto
    {
        /// <summary>
        /// Identificador único de la ubicación
        /// </summary>
        public int? IdUbicacion { get; set; }

        /// <summary>
        /// Nombre de la ubicación - REQUERIDO para POST
        /// </summary>
        public string? Nombre { get; set; }

        /// <summary>
        /// Descripción de la ubicación
        /// </summary>
        public string? Descripcion { get; set; }

        /// <summary>
        /// Coordenada latitud - REQUERIDO para POST
        /// </summary>
        public decimal? Latitud { get; set; }

        /// <summary>
        /// Coordenada longitud - REQUERIDO para POST
        /// </summary>
        public decimal? Longitud { get; set; }

        /// <summary>
        /// Dirección completa
        /// </summary>
        public string? DireccionCompleta { get; set; }

        /// <summary>
        /// Ciudad
        /// </summary>
        public string? Ciudad { get; set; }

        /// <summary>
        /// Estado o provincia
        /// </summary>
        public string? Estado { get; set; }

        /// <summary>
        /// País
        /// </summary>
        public string? Pais { get; set; }

        /// <summary>
        /// ID de lugar de Google Maps
        /// </summary>
        public string? PlaceId { get; set; }

        /// <summary>
        /// URL del icono para mostrar en mapa
        /// </summary>
        public string? Icono { get; set; }

        /// <summary>
        /// Color del icono (formato hexadecimal)
        /// </summary>
        public string? ColorIcono { get; set; }

        /// <summary>
        /// Nivel de zoom para Google Maps
        /// </summary>
        public int? NivelZoom { get; set; }

        /// <summary>
        /// HTML personalizado para ventana de información
        /// </summary>
        public string? InfoWindowHTML { get; set; }

        /// <summary>
        /// Categoría de la ubicación
        /// </summary>
        public string? Categoria { get; set; }

        /// <summary>
        /// Número de teléfono
        /// </summary>
        public string? Telefono { get; set; }

        /// <summary>
        /// Correo electrónico
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Metadata adicional en formato JSON
        /// </summary>
        public string? MetadataJSON { get; set; }

        /// <summary>
        /// Indica si la ubicación está activa
        /// </summary>
        public bool? Activo { get; set; }

        /// <summary>
        /// Usuario que creó el registro
        /// </summary>
        public string? UsuarioCreacion { get; set; }

        /// <summary>
        /// Usuario que modificó el registro
        /// </summary>
        public string? UsuarioModificacion { get; set; }
    }
}
