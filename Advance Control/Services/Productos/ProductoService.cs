using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;

namespace Advance_Control.Services.Productos
{
    /// <summary>
    /// Implementación del servicio de productos que se comunica con la API
    /// </summary>
    public class ProductoService : IProductoService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public ProductoService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<ProductoDto>> GetProductosAsync(ProductoQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "producto");

                if (query != null)
                {
                    url = new ApiQueryBuilder()
                        .Add("concepto", query.Concepto)
                        .Add("descripcion", query.Descripcion)
                        .Add("costoDirecto", query.CostoDirecto)
                        .Build(url);
                }

                await _logger.LogInformationAsync($"Obteniendo productos desde: {url}", "ProductoService", "GetProductosAsync");

                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al obtener productos. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "ProductoService",
                        "GetProductosAsync");
                    return new List<ProductoDto>();
                }

                var productos = await response.Content.ReadFromJsonAsync<List<ProductoDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);

                await _logger.LogInformationAsync($"Se obtuvieron {productos?.Count ?? 0} productos", "ProductoService", "GetProductosAsync");

                return productos ?? new List<ProductoDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al obtener productos", ex, "ProductoService", "GetProductosAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al obtener productos", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al obtener productos", ex, "ProductoService", "GetProductosAsync");
                throw;
            }
        }

        public async Task<bool> DeleteProductoAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "producto", id.ToString());

                await _logger.LogInformationAsync($"Eliminando producto {id} en: {url}", "ProductoService", "DeleteProductoAsync");

                using var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al eliminar producto. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "ProductoService",
                        "DeleteProductoAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Producto {id} eliminado correctamente", "ProductoService", "DeleteProductoAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al eliminar producto", ex, "ProductoService", "DeleteProductoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al eliminar producto", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al eliminar producto", ex, "ProductoService", "DeleteProductoAsync");
                throw;
            }
        }

        public async Task<bool> UpdateProductoAsync(int id, ProductoQueryDto query, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "producto", id.ToString());

                url = new ApiQueryBuilder()
                    .Add("concepto", query.Concepto)
                    .Add("descripcion", query.Descripcion)
                    .Add("costoDirecto", query.CostoDirecto)
                    .Add("porcentajeUtilidad", query.PorcentajeUtilidad)
                    .Build(url);

                await _logger.LogInformationAsync($"Actualizando producto {id} en: {url}", "ProductoService", "UpdateProductoAsync");

                using var response = await _http.PutAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al actualizar producto. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "ProductoService",
                        "UpdateProductoAsync");
                    return false;
                }

                await _logger.LogInformationAsync($"Producto {id} actualizado correctamente", "ProductoService", "UpdateProductoAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al actualizar producto", ex, "ProductoService", "UpdateProductoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al actualizar producto", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al actualizar producto", ex, "ProductoService", "UpdateProductoAsync");
                throw;
            }
        }

        public async Task<bool> CreateProductoAsync(string concepto, string descripcion, double costoDirecto, double porcentajeUtilidad, bool estatus = true, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "producto");

                url = new ApiQueryBuilder()
                    .Add("concepto", concepto)
                    .Add("descripcion", descripcion)
                    .AddRequired("costoDirecto", costoDirecto)
                    .AddRequired("porcentajeUtilidad", porcentajeUtilidad)
                    .AddRequired("estatus", estatus)
                    .Build(url);

                await _logger.LogInformationAsync($"Creando producto en: {url}", "ProductoService", "CreateProductoAsync");

                using var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _logger.LogErrorAsync(
                        $"Error al crear producto. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        "ProductoService",
                        "CreateProductoAsync");
                    return false;
                }

                await _logger.LogInformationAsync("Producto creado correctamente", "ProductoService", "CreateProductoAsync");
                return true;
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al crear producto", ex, "ProductoService", "CreateProductoAsync");
                throw new InvalidOperationException("Error de comunicación con el servidor al crear producto", ex);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error inesperado al crear producto", ex, "ProductoService", "CreateProductoAsync");
                throw;
            }
        }
    }
}
