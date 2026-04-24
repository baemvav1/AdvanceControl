using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.Services.Mensajeria
{
    public class MensajeriaService : IMensajeriaService
    {
        private readonly HttpClient _http;
        private readonly IApiEndpointProvider _endpoints;
        private readonly ILoggingService _logger;
        private HubConnection? _hubConnection;
        private string? _currentToken;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public bool EstaConectado => _hubConnection?.State == HubConnectionState.Connected;

        public long? UsuarioVisibleId { get; set; }

        public event EventHandler<MensajeDto>? MensajeRecibido;
        public event EventHandler<MensajeDto>? MensajeEnviado;
        public event EventHandler<long>? MensajeLeido;
        public event EventHandler<List<long>>? MensajesLeidos;
        public event EventHandler<List<long>>? MensajesEntregados;
        public event EventHandler<long>? MensajeEliminado;
        public event EventHandler<long>? MensajeOcultado;
        public event EventHandler<string>? UsuarioConectado;
        public event EventHandler<string>? UsuarioDesconectado;
        public event EventHandler<string>? UsuarioEscribiendo;
        public event EventHandler<bool>? EstadoConexionCambiado;

        public MensajeriaService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private void LogEventHandlerError(string handlerName, Exception ex)
        {
            _ = _logger.LogErrorAsync(
                $"Error en el handler de mensajería '{handlerName}'",
                ex,
                "MensajeriaService",
                handlerName);
        }

        public async Task ConectarAsync(string token, CancellationToken ct = default)
        {
            // Guard: si ya está conectado, no reconectar
            if (EstaConectado)
                return;

            if (_hubConnection != null)
                await DesconectarAsync();

            _currentToken = token;
            var hubUrl = _endpoints.GetApiBaseUrl().TrimEnd('/') + "/hubs/mensajes";

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(_currentToken);
                })
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                .Build();

            // Registrar handlers
            _hubConnection.On<MensajeDto>("RecibirMensaje", msg =>
            {
                try { MensajeRecibido?.Invoke(this, msg); }
                catch (Exception ex) { LogEventHandlerError("RecibirMensaje", ex); }
            });

            _hubConnection.On<MensajeDto>("MensajeEnviado", msg =>
            {
                try { MensajeEnviado?.Invoke(this, msg); }
                catch (Exception ex) { LogEventHandlerError("MensajeEnviado", ex); }
            });

            _hubConnection.On<long>("MensajeLeido", id =>
            {
                try { MensajeLeido?.Invoke(this, id); }
                catch (Exception ex) { LogEventHandlerError("MensajeLeido", ex); }
            });

            _hubConnection.On<List<long>>("MensajesLeidos", ids =>
            {
                try { MensajesLeidos?.Invoke(this, ids); }
                catch (Exception ex) { LogEventHandlerError("MensajesLeidos", ex); }
            });

            _hubConnection.On<List<long>>("MensajesEntregados", ids =>
            {
                try { MensajesEntregados?.Invoke(this, ids); }
                catch (Exception ex) { LogEventHandlerError("MensajesEntregados", ex); }
            });

            _hubConnection.On<long>("MensajeEliminado", id =>
            {
                try { MensajeEliminado?.Invoke(this, id); }
                catch (Exception ex) { LogEventHandlerError("MensajeEliminado", ex); }
            });

            _hubConnection.On<long>("MensajeOcultado", id =>
            {
                try { MensajeOcultado?.Invoke(this, id); }
                catch (Exception ex) { LogEventHandlerError("MensajeOcultado", ex); }
            });

            _hubConnection.On<string>("UsuarioConectado", id =>
            {
                try { UsuarioConectado?.Invoke(this, id); }
                catch (Exception ex) { LogEventHandlerError("UsuarioConectado", ex); }
            });

            _hubConnection.On<string>("UsuarioDesconectado", id =>
            {
                try { UsuarioDesconectado?.Invoke(this, id); }
                catch (Exception ex) { LogEventHandlerError("UsuarioDesconectado", ex); }
            });

            _hubConnection.On<string>("UsuarioEscribiendo", id =>
            {
                try { UsuarioEscribiendo?.Invoke(this, id); }
                catch (Exception ex) { LogEventHandlerError("UsuarioEscribiendo", ex); }
            });

            _hubConnection.Reconnecting += _ =>
            {
                EstadoConexionCambiado?.Invoke(this, false);
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += _ =>
            {
                EstadoConexionCambiado?.Invoke(this, true);
                return Task.CompletedTask;
            };

            _hubConnection.Closed += _ =>
            {
                EstadoConexionCambiado?.Invoke(this, false);
                return Task.CompletedTask;
            };

            try
            {
                await _hubConnection.StartAsync(ct);
                EstadoConexionCambiado?.Invoke(this, true);
                try { await _logger.LogInformationAsync("Conectado al hub de mensajería", "MensajeriaService", "ConectarAsync"); } catch { }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al conectar al hub de mensajería", ex, "MensajeriaService", "ConectarAsync");
                EstadoConexionCambiado?.Invoke(this, false);
                throw;
            }
        }

        public async Task DesconectarAsync()
        {
            if (_hubConnection != null)
            {
                try
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                }
                catch (Exception ex) { try { await _logger.LogWarningAsync($"Error al desconectar del hub de mensajería: {ex.Message}", "MensajeriaService", "DesconectarAsync"); } catch { } }
                finally
                {
                    _hubConnection = null;
                    EstadoConexionCambiado?.Invoke(this, false);
                }
            }
        }

        public async Task EnviarMensajeAsync(long paraCredencialId, string contenido, string tipo = "mensaje",
            int? idReferencia = null, string? tipoReferencia = null, string? fechaLimite = null,
            string? archivoUrl = null, long? respuestaAMensajeId = null)
        {
            if (_hubConnection == null)
            {
                await _logger.LogErrorAsync("No se puede enviar mensaje: hubConnection es null", null, "MensajeriaService", "EnviarMensajeAsync");
                return;
            }
            if (_hubConnection.State != HubConnectionState.Connected)
            {
                await _logger.LogErrorAsync($"No se puede enviar mensaje: estado={_hubConnection.State}", null, "MensajeriaService", "EnviarMensajeAsync");
                return;
            }
            try
            {
                await _hubConnection.InvokeAsync("EnviarMensaje", paraCredencialId, contenido, tipo,
                    idReferencia, tipoReferencia, fechaLimite, archivoUrl, respuestaAMensajeId);
            }
            catch (Exception ex)
            {
                try { await _logger.LogErrorAsync("Error en InvokeAsync EnviarMensaje", ex, "MensajeriaService", "EnviarMensajeAsync"); } catch { }
                throw;
            }
        }

        public async Task EliminarMensajeParaTodosAsync(long mensajeId)
        {
            if (_hubConnection?.State != HubConnectionState.Connected) return;
            try
            {
                await _hubConnection.InvokeAsync("EliminarMensajeParaTodos", mensajeId);
            }
            catch (Exception ex)
            {
                try { await _logger.LogErrorAsync("Error al eliminar mensaje para todos", ex, "MensajeriaService", "EliminarMensajeParaTodosAsync"); } catch { }
                throw;
            }
        }

        public async Task OcultarMensajeParaMiAsync(long mensajeId)
        {
            if (_hubConnection?.State != HubConnectionState.Connected) return;
            try
            {
                await _hubConnection.InvokeAsync("OcultarMensajeParaMi", mensajeId);
            }
            catch (Exception ex)
            {
                try { await _logger.LogErrorAsync("Error al ocultar mensaje para mí", ex, "MensajeriaService", "OcultarMensajeParaMiAsync"); } catch { }
                throw;
            }
        }

        public async Task MarcarLeidoAsync(long mensajeId)
        {
            if (_hubConnection?.State != HubConnectionState.Connected) return;
            await _hubConnection.InvokeAsync("MarcarLeido", mensajeId);
        }

        /// <summary>
        /// Marca como leídos en bloque todos los mensajes recibidos del otro usuario.
        /// Mucho más eficiente que invocar MarcarLeidoAsync por cada mensaje al abrir un chat.
        /// </summary>
        public async Task MarcarConversacionLeidaAsync(long otroCredencialId)
        {
            if (_hubConnection?.State != HubConnectionState.Connected) return;
            await _hubConnection.InvokeAsync("MarcarConversacionLeida", otroCredencialId);
        }

        public async Task EnviandoEscribiendoAsync(long paraCredencialId)
        {
            if (_hubConnection?.State != HubConnectionState.Connected) return;
            await _hubConnection.InvokeAsync("Escribiendo", paraCredencialId);
        }

        public async Task<List<string>> ObtenerUsuariosEnLineaAsync()
        {
            if (_hubConnection?.State != HubConnectionState.Connected)
                return new List<string>();
            return await _hubConnection.InvokeAsync<List<string>>("ObtenerUsuariosEnLinea");
        }

        // --- Métodos REST ---

        public async Task<List<MensajeDto>> GetConversacionAsync(long usuario1, long usuario2, CancellationToken ct = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "Mensajes", $"conversacion?usuario1={usuario1}&usuario2={usuario2}");
                return await _http.GetFromJsonAsync<List<MensajeDto>>(url, _jsonOptions, ct) ?? new();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al obtener conversación", ex, "MensajeriaService", "GetConversacionAsync");
                return new();
            }
        }

        public async Task<List<UsuarioChatDto>> GetUsuariosChatAsync(CancellationToken ct = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "Mensajes", "usuarios");
                return await _http.GetFromJsonAsync<List<UsuarioChatDto>>(url, _jsonOptions, ct) ?? new();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al obtener usuarios de chat", ex, "MensajeriaService", "GetUsuariosChatAsync");
                return new();
            }
        }

        public async Task<long> GetNoLeidosCountAsync(long credencialId, CancellationToken ct = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "Mensajes", $"no-leidos/{credencialId}");
                var result = await _http.GetFromJsonAsync<JsonElement>(url, ct);
                return result.GetProperty("count").GetInt64();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al obtener el conteo de mensajes no leídos", ex, "MensajeriaService", "GetNoLeidosCountAsync");
                return 0;
            }
        }

        public async Task<string?> UploadImagenMensajeAsync(long deCredencialId, long paraCredencialId,
            Stream imageStream, string fileName, string contentType, CancellationToken ct = default)
        {
            try
            {
                var url = _endpoints.GetEndpoint("api", "Uploads", $"mensajes?deCredencialId={deCredencialId}&paraCredencialId={paraCredencialId}");

                using var content = new MultipartFormDataContent();
                var streamContent = new StreamContent(imageStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                content.Add(streamContent, "file", fileName);

                var response = await _http.PostAsync(url, content, ct);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
                return result.GetProperty("url").GetString();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al subir imagen de mensaje", ex, "MensajeriaService", "UploadImagenMensajeAsync");
                return null;
            }
        }
    }
}
