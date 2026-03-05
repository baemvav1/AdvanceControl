using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;

namespace Advance_Control.Services.CheckOperacion
{
    public class CheckOperacionService : ICheckOperacionService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly JsonSerializerOptions _jsonOptions;

        public CheckOperacionService(HttpClient http, IApiEndpointProvider endpoints)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<CheckOperacionDto?> GetAsync(int idOperacion)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "CheckOperacion", idOperacion.ToString());
                var response = await _http.GetAsync(url);

                if (!response.IsSuccessStatusCode) return null;

                return await response.Content.ReadFromJsonAsync<CheckOperacionDto>(_jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> UpdateCampoAsync(int idOperacion, string campo, bool valor = true)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "CheckOperacion", idOperacion.ToString());
                var payload = new { campo, valor };
                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await _http.PutAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
