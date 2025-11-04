using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Security;

namespace Advance_Control.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ISecureStorage _secureStorage;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);
        private readonly Task _initTask;

        private volatile bool _isAuthenticated;
        private string? _accessToken;
        private string? _refreshToken;
        private DateTime? _accessExpiresAtUtc;

        private const string Key_AccessToken = "auth.access_token";
        private const string Key_RefreshToken = "auth.refresh_token";
        private const string Key_AccessExpiresAt = "auth.access_expires_at_utc";

        public bool IsAuthenticated => _isAuthenticated;

        public AuthService(HttpClient http, IApiEndpointProvider endpoints, ISecureStorage secureStorage)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));
            _initTask = LoadFromStorageAsync();
        }

        private async Task LoadFromStorageAsync()
        {
            try
            {
                _accessToken = await _secureStorage.GetAsync(Key_AccessToken);
                _refreshToken = await _secureStorage.GetAsync(Key_RefreshToken);
                var expiresText = await _secureStorage.GetAsync(Key_AccessExpiresAt);
                if (DateTime.TryParse(expiresText, out var dt)) _accessExpiresAtUtc = dt;
                _isAuthenticated = !string.IsNullOrEmpty(_accessToken) && _accessExpiresAtUtc.HasValue && _accessExpiresAtUtc > DateTime.UtcNow;
            }
            catch
            {
                // ignore storage errors, treat as not authenticated
                _isAuthenticated = false;
            }
        }

        public async Task<bool> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            // Ensure initialization is complete
            await _initTask.ConfigureAwait(false);
            
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            var url = _endpoints.GetEndpoint("api", "Auth", "login");
            var body = new { usuario = username, pass = password }; // matches server controller

            try
            {
                var resp = await _http.PostAsJsonAsync(url, body, cancellationToken);
                if (!resp.IsSuccessStatusCode) return false;

                var dto = await resp.Content.ReadFromJsonAsync<LoginResponseDto>(cancellationToken: cancellationToken);
                if (dto == null || string.IsNullOrEmpty(dto.accessToken)) return false;

                _accessToken = dto.accessToken;
                _refreshToken = dto.refreshToken;
                _accessExpiresAtUtc = DateTime.UtcNow.AddSeconds(dto.expiresIn);

                await PersistTokensAsync();
                _isAuthenticated = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            // Ensure initialization is complete
            await _initTask.ConfigureAwait(false);
            
            if (!string.IsNullOrEmpty(_accessToken) && _accessExpiresAtUtc.HasValue && _accessExpiresAtUtc > DateTime.UtcNow.AddSeconds(15))
                return _accessToken;

            var ok = await RefreshTokenAsync(cancellationToken);
            return ok ? _accessToken : null;
        }

        public async Task<bool> RefreshTokenAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_refreshToken))
            {
                _refreshToken = await _secureStorage.GetAsync(Key_RefreshToken);
            }

            if (string.IsNullOrEmpty(_refreshToken)) return false;

            await _refreshLock.WaitAsync(cancellationToken);
            try
            {
                if (!string.IsNullOrEmpty(_accessToken) && _accessExpiresAtUtc.HasValue && _accessExpiresAtUtc > DateTime.UtcNow.AddSeconds(15))
                    return true;

                var url = _endpoints.GetEndpoint("api", "Auth", "refresh");
                var body = new { refreshToken = _refreshToken };

                var resp = await _http.PostAsJsonAsync(url, body, cancellationToken);
                if (!resp.IsSuccessStatusCode)
                {
                    if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        await ClearTokenAsync();
                        return false;
                    }
                    return false;
                }

                var dto = await resp.Content.ReadFromJsonAsync<RefreshResponseDto>(cancellationToken: cancellationToken);
                if (dto == null || string.IsNullOrEmpty(dto.accessToken)) return false;

                _accessToken = dto.accessToken;
                _refreshToken = dto.refreshToken ?? _refreshToken;
                _accessExpiresAtUtc = DateTime.UtcNow.AddSeconds(dto.expiresIn);

                await PersistTokensAsync();
                _isAuthenticated = true;
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        public async Task<bool> ValidateTokenAsync(CancellationToken cancellationToken = default)
        {
            var token = await GetAccessTokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(token)) return false;

            try
            {
                var url = _endpoints.GetEndpoint("api", "Auth", "validate");
                var body = new { token = token };
                var resp = await _http.PostAsJsonAsync(url, body, cancellationToken);
                if (resp.IsSuccessStatusCode) return true;
                if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return await RefreshTokenAsync(cancellationToken);
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task ClearTokenAsync()
        {
            _accessToken = null;
            _refreshToken = null;
            _accessExpiresAtUtc = null;
            _isAuthenticated = false;

            try
            {
                await _secureStorage.RemoveAsync(Key_AccessToken);
                await _secureStorage.RemoveAsync(Key_RefreshToken);
                await _secureStorage.RemoveAsync(Key_AccessExpiresAt);
            }
            catch
            {
                // ignore storage errors
            }
        }

        private async Task PersistTokensAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(_accessToken))
                    await _secureStorage.SetAsync(Key_AccessToken, _accessToken);
                if (!string.IsNullOrEmpty(_refreshToken))
                    await _secureStorage.SetAsync(Key_RefreshToken, _refreshToken);
                if (_accessExpiresAtUtc.HasValue)
                    await _secureStorage.SetAsync(Key_AccessExpiresAt, _accessExpiresAtUtc.Value.ToString("o")); // ISO
            }
            catch
            {
                // ignore
            }
        }

        // DTOs to match server responses
        private class LoginResponseDto
        {
            public string? accessToken { get; set; }
            public string? refreshToken { get; set; }
            public int expiresIn { get; set; }
            public string? tokenType { get; set; }
            public object? user { get; set; }
        }

        private class RefreshResponseDto
        {
            public string? accessToken { get; set; }
            public string? refreshToken { get; set; }
            public int expiresIn { get; set; }
        }
    }
}