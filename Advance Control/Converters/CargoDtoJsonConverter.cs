using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Advance_Control.Models;

namespace Advance_Control.Converters
{
    /// <summary>
    /// Custom JSON converter for CargoDto to handle multiple possible field names for IdRelacionCargo
    /// This handles potential typos or variations in the backend API response
    /// </summary>
    public class CargoDtoJsonConverter : JsonConverter<CargoDto>
    {
        public override CargoDto Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token");
            }

            var cargo = new CargoDto();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return cargo;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected PropertyName token");
                }

                string propertyName = reader.GetString() ?? string.Empty;
                reader.Read();

                // Handle each property with case-insensitive comparison
                if (propertyName.Equals("idCargo", StringComparison.OrdinalIgnoreCase))
                {
                    cargo.IdCargo = reader.GetInt32();
                }
                else if (propertyName.Equals("idTipoCargo", StringComparison.OrdinalIgnoreCase))
                {
                    cargo.IdTipoCargo = reader.TokenType == JsonTokenType.Null ? null : reader.GetInt32();
                }
                else if (propertyName.Equals("idOperacion", StringComparison.OrdinalIgnoreCase))
                {
                    cargo.IdOperacion = reader.TokenType == JsonTokenType.Null ? null : reader.GetInt32();
                }
                // Handle multiple possible names for IdRelacionCargo (with potential typo)
                else if (propertyName.Equals("idRelacionCargo", StringComparison.OrdinalIgnoreCase) ||
                         propertyName.Equals("idReclacionCargo", StringComparison.OrdinalIgnoreCase))
                {
                    cargo.IdRelacionCargo = reader.TokenType == JsonTokenType.Null ? null : reader.GetInt32();
                }
                else if (propertyName.Equals("monto", StringComparison.OrdinalIgnoreCase))
                {
                    cargo.Monto = reader.TokenType == JsonTokenType.Null ? null : reader.GetDouble();
                }
                else if (propertyName.Equals("nota", StringComparison.OrdinalIgnoreCase))
                {
                    cargo.Nota = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                }
                else if (propertyName.Equals("detalleRelacionado", StringComparison.OrdinalIgnoreCase))
                {
                    cargo.DetalleRelacionado = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                }
                else if (propertyName.Equals("tipoCargo", StringComparison.OrdinalIgnoreCase))
                {
                    cargo.TipoCargo = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                }
                else if (propertyName.Equals("proveedor", StringComparison.OrdinalIgnoreCase))
                {
                    cargo.Proveedor = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                }
                else if (propertyName.Equals("cantidad", StringComparison.OrdinalIgnoreCase))
                {
                    cargo.Cantidad = reader.TokenType == JsonTokenType.Null ? null : reader.GetDouble();
                }
                else if (propertyName.Equals("unitario", StringComparison.OrdinalIgnoreCase))
                {
                    cargo.Unitario = reader.TokenType == JsonTokenType.Null ? null : reader.GetDouble();
                }
                else
                {
                    // Skip unknown properties
                    reader.Skip();
                }
            }

            throw new JsonException("Expected EndObject token");
        }

        public override void Write(Utf8JsonWriter writer, CargoDto value, JsonSerializerOptions options)
        {
            // Use default serialization for writing
            writer.WriteStartObject();
            
            writer.WriteNumber("idCargo", value.IdCargo);
            
            if (value.IdTipoCargo.HasValue)
                writer.WriteNumber("idTipoCargo", value.IdTipoCargo.Value);
            else
                writer.WriteNull("idTipoCargo");
            
            if (value.IdOperacion.HasValue)
                writer.WriteNumber("idOperacion", value.IdOperacion.Value);
            else
                writer.WriteNull("idOperacion");
            
            if (value.IdRelacionCargo.HasValue)
                writer.WriteNumber("idRelacionCargo", value.IdRelacionCargo.Value);
            else
                writer.WriteNull("idRelacionCargo");
            
            if (value.Monto.HasValue)
                writer.WriteNumber("monto", value.Monto.Value);
            else
                writer.WriteNull("monto");
            
            if (!string.IsNullOrEmpty(value.Nota))
                writer.WriteString("nota", value.Nota);
            else
                writer.WriteNull("nota");
            
            if (!string.IsNullOrEmpty(value.DetalleRelacionado))
                writer.WriteString("detalleRelacionado", value.DetalleRelacionado);
            else
                writer.WriteNull("detalleRelacionado");
            
            if (!string.IsNullOrEmpty(value.TipoCargo))
                writer.WriteString("tipoCargo", value.TipoCargo);
            else
                writer.WriteNull("tipoCargo");
            
            if (!string.IsNullOrEmpty(value.Proveedor))
                writer.WriteString("proveedor", value.Proveedor);
            else
                writer.WriteNull("proveedor");
            
            if (value.Cantidad.HasValue)
                writer.WriteNumber("cantidad", value.Cantidad.Value);
            else
                writer.WriteNull("cantidad");
            
            if (value.Unitario.HasValue)
                writer.WriteNumber("unitario", value.Unitario.Value);
            else
                writer.WriteNull("unitario");
            
            writer.WriteEndObject();
        }
    }
}
