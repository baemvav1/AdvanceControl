using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Security;
using Advance_Control.Settings;
using Microsoft.Extensions.Options;

namespace Advance_Control.Services.GoogleCloudStorage
{
    /// <summary>
    /// Implementación del servicio de autenticación OAuth 2.0 con Google Cloud Storage.
    /// Utiliza el flujo de autorización de código con PKCE para aplicaciones de escritorio.
    /// </summary>
    public class GoogleCloudStorageAuthService : IGoogleCloudStorageAuthService
    {
        private readonly HttpClient _http;
        private readonly ISecureStorage _secureStorage;
        private readonly ILoggingService _logger;
        private readonly GoogleCloudStorageOptions _options;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);
        private readonly Task _initTask;

        // OAuth 2.0 endpoints de Google
        private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string TokenEndpoint = "https://oauth2.googleapis.com/token";

        // Scopes requeridos para Google Cloud Storage
        private const string Scopes = "https://www.googleapis.com/auth/devstorage.read_write";

        // Claves para almacenamiento seguro
        private const string Key_AccessToken = "gcs.access_token";
        private const string Key_RefreshToken = "gcs.refresh_token";
        private const string Key_AccessExpiresAt = "gcs.access_expires_at_utc";

        private volatile bool _isAuthenticated;
        private string? _accessToken;
        private string? _refreshToken;
        private DateTime? _accessExpiresAtUtc;

        public bool IsAuthenticated => _isAuthenticated;

        public GoogleCloudStorageAuthService(
            HttpClient http,
            ISecureStorage secureStorage,
            ILoggingService logger,
            IOptions<GoogleCloudStorageOptions> options)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
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

                _isAuthenticated = !string.IsNullOrEmpty(_accessToken) && 
                                   _accessExpiresAtUtc.HasValue && 
                                   _accessExpiresAtUtc > DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al cargar tokens de GCS desde almacenamiento seguro", ex, "GoogleCloudStorageAuthService", "LoadFromStorageAsync");
                _isAuthenticated = false;
            }
        }

        /// <summary>
        /// Obtiene un token de acceso válido para Google Cloud Storage
        /// </summary>
        public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            await _initTask.ConfigureAwait(false);

            // Si el token aún es válido (con margen de 60 segundos), devolverlo
            if (!string.IsNullOrEmpty(_accessToken) && 
                _accessExpiresAtUtc.HasValue && 
                _accessExpiresAtUtc > DateTime.UtcNow.AddSeconds(60))
            {
                return _accessToken;
            }

            // Intentar refrescar el token
            if (await RefreshTokenAsync(cancellationToken))
            {
                return _accessToken;
            }

            await _logger.LogWarningAsync("No se pudo obtener token de acceso para GCS", "GoogleCloudStorageAuthService", "GetAccessTokenAsync");
            return null;
        }

        /// <summary>
        /// Inicia el flujo de autenticación OAuth 2.0 con Google
        /// </summary>
        public async Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default)
        {
            var result = await AuthenticateWithResultAsync(cancellationToken);
            return result.Success;
        }

        /// <summary>
        /// Inicia el flujo de autenticación OAuth 2.0 con Google y devuelve información detallada del resultado.
        /// Útil para manejar errores específicos como 'org_internal' (cliente configurado solo para uso interno).
        /// </summary>
        public async Task<GoogleCloudStorageAuthResult> AuthenticateWithResultAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync("Iniciando flujo de autenticación OAuth 2.0 con Google", "GoogleCloudStorageAuthService", "AuthenticateWithResultAsync");

                // Generar code verifier y challenge para PKCE
                var codeVerifier = GenerateCodeVerifier();
                var codeChallenge = GenerateCodeChallenge(codeVerifier);
                var state = GenerateRandomString(32);

                // Construir URL de autorización
                var authUrl = $"{AuthorizationEndpoint}?" +
                    $"client_id={Uri.EscapeDataString(_options.ClientId)}" +
                    $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}" +
                    $"&response_type=code" +
                    $"&scope={Uri.EscapeDataString(Scopes)}" +
                    $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
                    $"&code_challenge_method=S256" +
                    $"&state={Uri.EscapeDataString(state)}" +
                    $"&access_type=offline" +
                    $"&prompt=consent";

                // Iniciar listener HTTP local para recibir el callback
                using var listener = new HttpListener();
                listener.Prefixes.Add(_options.RedirectUri);
                listener.Start();

                // Abrir navegador para autenticación
                Process.Start(new ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                });

                await _logger.LogInformationAsync("Esperando respuesta de autorización de Google...", "GoogleCloudStorageAuthService", "AuthenticateWithResultAsync");

                // Esperar el callback con el código de autorización
                var context = await listener.GetContextAsync().WaitAsync(cancellationToken);
                var request = context.Request;
                var response = context.Response;

                // Parsear parámetros de la respuesta
                var query = request.Url?.Query ?? string.Empty;
                var queryParams = ParseQueryString(query);

                // Verificar state
                if (!queryParams.TryGetValue("state", out var returnedState) || returnedState != state)
                {
                    await SendHtmlResponse(response, "Error: Estado inválido. Por favor, intente nuevamente.", false);
                    await _logger.LogErrorAsync("Estado OAuth no coincide", null, "GoogleCloudStorageAuthService", "AuthenticateWithResultAsync");
                    return GoogleCloudStorageAuthResult.Failed("state_mismatch", "El estado de la solicitud OAuth no coincide");
                }

                // Verificar error - manejar errores específicos de OAuth
                if (queryParams.TryGetValue("error", out var error))
                {
                    var errorDescription = queryParams.TryGetValue("error_description", out var desc) ? desc : error;
                    var result = GoogleCloudStorageAuthResult.Failed(error, errorDescription);
                    
                    await SendHtmlResponse(response, result.UserFriendlyMessage ?? $"Error de autenticación: {error}", false);
                    await _logger.LogErrorAsync($"Error en autorización OAuth: {error} - {errorDescription}", null, "GoogleCloudStorageAuthService", "AuthenticateWithResultAsync");
                    return result;
                }

                // Obtener código de autorización
                if (!queryParams.TryGetValue("code", out var authCode))
                {
                    await SendHtmlResponse(response, "Error: No se recibió código de autorización.", false);
                    await _logger.LogErrorAsync("No se recibió código de autorización", null, "GoogleCloudStorageAuthService", "AuthenticateWithResultAsync");
                    return GoogleCloudStorageAuthResult.Failed("no_code", "No se recibió el código de autorización de Google");
                }

                // Enviar respuesta de éxito al navegador
                await SendHtmlResponse(response, "¡Autenticación exitosa! Puede cerrar esta ventana.", true);
                listener.Stop();

                // Intercambiar código por tokens
                var tokenSuccess = await ExchangeCodeForTokensAsync(authCode, codeVerifier, cancellationToken);
                if (tokenSuccess)
                {
                    return GoogleCloudStorageAuthResult.Succeeded();
                }
                return GoogleCloudStorageAuthResult.Failed("token_exchange_failed", "No se pudieron obtener los tokens de acceso");
            }
            catch (OperationCanceledException)
            {
                await _logger.LogWarningAsync("Autenticación OAuth cancelada por el usuario", "GoogleCloudStorageAuthService", "AuthenticateWithResultAsync");
                return GoogleCloudStorageAuthResult.Failed("cancelled", "La autenticación fue cancelada por el usuario");
            }
            catch (TimeoutException)
            {
                await _logger.LogWarningAsync("Autenticación OAuth excedió el tiempo de espera", "GoogleCloudStorageAuthService", "AuthenticateWithResultAsync");
                return GoogleCloudStorageAuthResult.Failed("timeout", "La autenticación excedió el tiempo de espera");
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error durante la autenticación OAuth", ex, "GoogleCloudStorageAuthService", "AuthenticateWithResultAsync");
                return GoogleCloudStorageAuthResult.Failed("unknown", ex.Message);
            }
        }

        /// <summary>
        /// Intercambia el código de autorización por tokens
        /// </summary>
        private async Task<bool> ExchangeCodeForTokensAsync(string authCode, string codeVerifier, CancellationToken cancellationToken)
        {
            try
            {
                var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["code"] = authCode,
                    ["client_id"] = _options.ClientId,
                    ["client_secret"] = _options.ClientSecret,
                    ["redirect_uri"] = _options.RedirectUri,
                    ["grant_type"] = "authorization_code",
                    ["code_verifier"] = codeVerifier
                });

                var response = await _http.PostAsync(TokenEndpoint, tokenRequest, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    await _logger.LogErrorAsync($"Error al intercambiar código por tokens: {errorContent}", null, "GoogleCloudStorageAuthService", "ExchangeCodeForTokensAsync");
                    return false;
                }

                var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponseDto>(cancellationToken: cancellationToken);

                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.access_token))
                {
                    await _logger.LogErrorAsync("Respuesta de token inválida", null, "GoogleCloudStorageAuthService", "ExchangeCodeForTokensAsync");
                    return false;
                }

                _accessToken = tokenResponse.access_token;
                _refreshToken = tokenResponse.refresh_token;
                _accessExpiresAtUtc = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in);

                await PersistTokensAsync();
                _isAuthenticated = true;

                await _logger.LogInformationAsync("Tokens de GCS obtenidos exitosamente", "GoogleCloudStorageAuthService", "ExchangeCodeForTokensAsync");
                return true;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al intercambiar código por tokens", ex, "GoogleCloudStorageAuthService", "ExchangeCodeForTokensAsync");
                return false;
            }
        }

        /// <summary>
        /// Refresca el token de acceso usando el refresh token
        /// </summary>
        private async Task<bool> RefreshTokenAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_refreshToken))
            {
                _refreshToken = await _secureStorage.GetAsync(Key_RefreshToken);
            }

            if (string.IsNullOrEmpty(_refreshToken))
            {
                await _logger.LogWarningAsync("No hay refresh token disponible para GCS", "GoogleCloudStorageAuthService", "RefreshTokenAsync");
                return false;
            }

            await _refreshLock.WaitAsync(cancellationToken);
            try
            {
                // Verificar si otro hilo ya refrescó el token
                if (!string.IsNullOrEmpty(_accessToken) && 
                    _accessExpiresAtUtc.HasValue && 
                    _accessExpiresAtUtc > DateTime.UtcNow.AddSeconds(60))
                {
                    return true;
                }

                await _logger.LogInformationAsync("Refrescando token de acceso de GCS...", "GoogleCloudStorageAuthService", "RefreshTokenAsync");

                var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = _options.ClientId,
                    ["client_secret"] = _options.ClientSecret,
                    ["refresh_token"] = _refreshToken,
                    ["grant_type"] = "refresh_token"
                });

                var response = await _http.PostAsync(TokenEndpoint, tokenRequest, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    await _logger.LogErrorAsync($"Error al refrescar token de GCS: {errorContent}", null, "GoogleCloudStorageAuthService", "RefreshTokenAsync");
                    
                    // Si el refresh token es inválido, limpiar autenticación
                    if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        await ClearAuthenticationAsync();
                    }
                    return false;
                }

                var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponseDto>(cancellationToken: cancellationToken);

                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.access_token))
                {
                    await _logger.LogErrorAsync("Respuesta de refresh token inválida", null, "GoogleCloudStorageAuthService", "RefreshTokenAsync");
                    return false;
                }

                _accessToken = tokenResponse.access_token;
                // El refresh token puede no venir en la respuesta de refresh
                if (!string.IsNullOrEmpty(tokenResponse.refresh_token))
                {
                    _refreshToken = tokenResponse.refresh_token;
                }
                _accessExpiresAtUtc = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in);

                await PersistTokensAsync();
                _isAuthenticated = true;

                await _logger.LogInformationAsync("Token de GCS refrescado exitosamente", "GoogleCloudStorageAuthService", "RefreshTokenAsync");
                return true;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al refrescar token de GCS", ex, "GoogleCloudStorageAuthService", "RefreshTokenAsync");
                return false;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        /// <summary>
        /// Limpia los tokens almacenados
        /// </summary>
        public async Task ClearAuthenticationAsync()
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
                await _logger.LogInformationAsync("Tokens de GCS limpiados", "GoogleCloudStorageAuthService", "ClearAuthenticationAsync");
            }
            catch (Exception ex)
            {
                await _logger.LogWarningAsync($"Error al limpiar tokens de GCS: {ex.Message}", "GoogleCloudStorageAuthService", "ClearAuthenticationAsync");
            }
        }

        /// <summary>
        /// Intenta restaurar la sesión desde tokens almacenados
        /// </summary>
        public async Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default)
        {
            await _initTask.ConfigureAwait(false);

            if (string.IsNullOrEmpty(_accessToken) && string.IsNullOrEmpty(_refreshToken))
            {
                await _logger.LogInformationAsync("No hay tokens de GCS almacenados para restaurar", "GoogleCloudStorageAuthService", "TryRestoreSessionAsync");
                return false;
            }

            // Si hay access token válido
            if (!string.IsNullOrEmpty(_accessToken) && 
                _accessExpiresAtUtc.HasValue && 
                _accessExpiresAtUtc > DateTime.UtcNow.AddSeconds(60))
            {
                _isAuthenticated = true;
                await _logger.LogInformationAsync("Sesión de GCS restaurada con token válido", "GoogleCloudStorageAuthService", "TryRestoreSessionAsync");
                return true;
            }

            // Intentar refrescar
            if (!string.IsNullOrEmpty(_refreshToken))
            {
                var refreshed = await RefreshTokenAsync(cancellationToken);
                if (refreshed)
                {
                    await _logger.LogInformationAsync("Sesión de GCS restaurada mediante refresh token", "GoogleCloudStorageAuthService", "TryRestoreSessionAsync");
                    return true;
                }
            }

            await _logger.LogInformationAsync("No se pudo restaurar sesión de GCS", "GoogleCloudStorageAuthService", "TryRestoreSessionAsync");
            return false;
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
                    await _secureStorage.SetAsync(Key_AccessExpiresAt, _accessExpiresAtUtc.Value.ToString("o"));
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al persistir tokens de GCS", ex, "GoogleCloudStorageAuthService", "PersistTokensAsync");
            }
        }

        /// <summary>
        /// Genera un code verifier aleatorio para PKCE (43-128 caracteres)
        /// </summary>
        private static string GenerateCodeVerifier()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Base64UrlEncode(bytes);
        }

        /// <summary>
        /// Genera el code challenge a partir del verifier usando SHA256
        /// </summary>
        private static string GenerateCodeChallenge(string codeVerifier)
        {
            using var sha256 = SHA256.Create();
            var challengeBytes = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
            return Base64UrlEncode(challengeBytes);
        }

        /// <summary>
        /// Genera una cadena aleatoria segura
        /// </summary>
        private static string GenerateRandomString(int length)
        {
            // Generate enough bytes to ensure Base64 output is at least 'length' characters
            // Base64 encodes 3 bytes to 4 characters, so we need at least (length * 3 / 4) bytes
            var bytesNeeded = (int)Math.Ceiling(length * 3.0 / 4.0) + 1;
            var bytes = RandomNumberGenerator.GetBytes(bytesNeeded);
            var encoded = Base64UrlEncode(bytes);
            // Ensure we don't exceed the encoded string length
            return encoded.Length >= length ? encoded.Substring(0, length) : encoded;
        }

        /// <summary>
        /// Codifica bytes en Base64 URL-safe
        /// </summary>
        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        /// <summary>
        /// Parsea una query string en un diccionario
        /// </summary>
        private static Dictionary<string, string> ParseQueryString(string query)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(query)) return result;

            query = query.TrimStart('?');
            foreach (var pair in query.Split('&'))
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2)
                {
                    result[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
                }
            }
            return result;
        }

        /// <summary>
        /// Envía una respuesta HTML al navegador
        /// </summary>
        private static async Task SendHtmlResponse(HttpListenerResponse response, string message, bool success)
        {
            var color = success ? "#4CAF50" : "#f44336";
            var icon = success ? "✓" : "✗";
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Advance Control - Autenticación Google Cloud</title>
    <style>
        body {{ 
            font-family: 'Segoe UI', Arial, sans-serif; 
            display: flex; 
            justify-content: center; 
            align-items: center; 
            height: 100vh; 
            margin: 0;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        }}
        .container {{ 
            background: white; 
            padding: 40px 60px; 
            border-radius: 16px; 
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            text-align: center;
        }}
        .icon {{ 
            font-size: 64px; 
            color: {color}; 
            margin-bottom: 20px;
        }}
        h1 {{ 
            color: #333; 
            margin: 0 0 10px 0;
            font-size: 24px;
        }}
        p {{ 
            color: #666; 
            margin: 0;
            font-size: 16px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='icon'>{icon}</div>
        <h1>Advance Control</h1>
        <p>{message}</p>
    </div>
</body>
</html>";

            var buffer = Encoding.UTF8.GetBytes(html);
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.Close();
        }

        /// <summary>
        /// DTO para la respuesta de token de OAuth 2.0
        /// </summary>
        private class TokenResponseDto
        {
            public string? access_token { get; set; }
            public string? refresh_token { get; set; }
            public int expires_in { get; set; }
            public string? token_type { get; set; }
            public string? scope { get; set; }
        }
    }
}
