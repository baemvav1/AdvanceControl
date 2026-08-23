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

namespace Advance_Control.Services.SatCatalogo
{
    public class SatCatalogoService : ISatCatalogoService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;

        public SatCatalogoService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<SatClaveDto>> BuscarClaveProdServAsync(string? texto, CancellationToken cancellationToken = default)
        {
            var url = new ApiQueryBuilder()
                .Add("texto", texto)
                .Build(_endpoints.GetEndpoint("api", "sat-catalogo", "clave-prod-serv"));
            return await BuscarAsync(url, cancellationToken);
        }

        public async Task<SatClaveDto?> AgregarClaveProdServAsync(string clave, string descripcion, CancellationToken cancellationToken = default)
        {
            var url = new ApiQueryBuilder()
                .Add("clave", clave)
                .Add("descripcion", descripcion)
                .Build(_endpoints.GetEndpoint("api", "sat-catalogo", "clave-prod-serv"));
            return await AgregarAsync(url, cancellationToken);
        }

        public async Task<List<SatClaveDto>> BuscarClaveUnidadAsync(string? texto, CancellationToken cancellationToken = default)
        {
            var url = new ApiQueryBuilder()
                .Add("texto", texto)
                .Build(_endpoints.GetEndpoint("api", "sat-catalogo", "clave-unidad"));
            return await BuscarAsync(url, cancellationToken);
        }

        public async Task<SatClaveDto?> AgregarClaveUnidadAsync(string clave, string nombre, CancellationToken cancellationToken = default)
        {
            var url = new ApiQueryBuilder()
                .Add("clave", clave)
                .Add("nombre", nombre)
                .Build(_endpoints.GetEndpoint("api", "sat-catalogo", "clave-unidad"));
            return await AgregarAsync(url, cancellationToken);
        }

        public Task<List<SatCatalogoItemDto>> ListarRegimenFiscalAsync(bool incluirInactivos = false, CancellationToken cancellationToken = default) =>
            ListarCatalogoItemAsync("regimen-fiscal", incluirInactivos, cancellationToken);

        public Task<SatCatalogoItemDto?> GuardarRegimenFiscalAsync(SatCatalogoItemDto item, CancellationToken cancellationToken = default) =>
            GuardarCatalogoItemAsync("regimen-fiscal", item, cancellationToken);

        public Task<bool> InactivarRegimenFiscalAsync(string clave, bool estatus, CancellationToken cancellationToken = default) =>
            InactivarCatalogoItemAsync("regimen-fiscal", clave, estatus, cancellationToken);

        public Task<List<SatCatalogoItemDto>> ListarUsoCfdiAsync(bool incluirInactivos = false, CancellationToken cancellationToken = default) =>
            ListarCatalogoItemAsync("uso-cfdi", incluirInactivos, cancellationToken);

        public Task<SatCatalogoItemDto?> GuardarUsoCfdiAsync(SatCatalogoItemDto item, CancellationToken cancellationToken = default) =>
            GuardarCatalogoItemAsync("uso-cfdi", item, cancellationToken);

        public Task<bool> InactivarUsoCfdiAsync(string clave, bool estatus, CancellationToken cancellationToken = default) =>
            InactivarCatalogoItemAsync("uso-cfdi", clave, estatus, cancellationToken);

        private async Task<List<SatCatalogoItemDto>> ListarCatalogoItemAsync(string recurso, bool incluirInactivos, CancellationToken cancellationToken)
        {
            var url = new ApiQueryBuilder()
                .Add("incluirInactivos", incluirInactivos)
                .Build(_endpoints.GetEndpoint("api", "sat-catalogo", recurso));

            try
            {
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return new List<SatCatalogoItemDto>();

                var resultado = await response.Content.ReadFromJsonAsync<List<SatCatalogoItemDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);
                return resultado ?? new List<SatCatalogoItemDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync($"Error de red al listar catálogo SAT '{recurso}'", ex, "SatCatalogoService", "ListarCatalogoItemAsync");
                return new List<SatCatalogoItemDto>();
            }
        }

        private async Task<SatCatalogoItemDto?> GuardarCatalogoItemAsync(string recurso, SatCatalogoItemDto item, CancellationToken cancellationToken)
        {
            var url = _endpoints.GetEndpoint("api", "sat-catalogo", recurso);
            using var response = await _http.PostAsJsonAsync(url, item, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                await _logger.LogWarningAsync($"API rechazó guardar '{recurso}': {errorContent}", "SatCatalogoService", "GuardarCatalogoItemAsync");
                return null;
            }
            return await response.Content.ReadFromJsonAsync<SatCatalogoItemDto>(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private async Task<bool> InactivarCatalogoItemAsync(string recurso, string clave, bool estatus, CancellationToken cancellationToken)
        {
            var url = new ApiQueryBuilder()
                .Add("estatus", estatus)
                .Build($"{_endpoints.GetEndpoint("api", "sat-catalogo", recurso)}/{Uri.EscapeDataString(clave)}");

            using var response = await _http.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }

        private async Task<List<SatClaveDto>> BuscarAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return new List<SatClaveDto>();

                var resultado = await response.Content.ReadFromJsonAsync<List<SatClaveDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);
                return resultado ?? new List<SatClaveDto>();
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogErrorAsync("Error de red al buscar catálogo SAT", ex, "SatCatalogoService", "BuscarAsync");
                return new List<SatClaveDto>();
            }
        }

        private async Task<SatClaveDto?> AgregarAsync(string url, CancellationToken cancellationToken)
        {
            using var response = await _http.PostAsync(url, null, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                await _logger.LogWarningAsync($"API rechazó agregar clave SAT: {errorContent}", "SatCatalogoService", "AgregarAsync");
                return null;
            }
            return await response.Content.ReadFromJsonAsync<SatClaveDto>(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
