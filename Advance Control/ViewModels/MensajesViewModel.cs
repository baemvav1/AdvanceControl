using Advance_Control.Models;
using Advance_Control.Services.Auth;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Mensajeria;
using Advance_Control.Services.Session;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.ViewModels
{
    public class MensajesViewModel : ViewModelBase
    {
        private readonly IMensajeriaService _mensajeria;
        private readonly IUserSessionService _session;
        private readonly IAuthService _authService;
        private readonly ILoggingService _logger;
        private readonly IApiEndpointProvider _endpoints;
        private DispatcherQueue? _dispatcher;

        public ObservableCollection<UsuarioChatDto> Usuarios { get; } = new();
        public ObservableCollection<MensajeDto> Mensajes { get; } = new();

        private UsuarioChatDto? _usuarioSeleccionado;
        public UsuarioChatDto? UsuarioSeleccionado
        {
            get => _usuarioSeleccionado;
            set
            {
                if (SetProperty(ref _usuarioSeleccionado, value))
                {
                    OnPropertyChanged(nameof(HayUsuarioSeleccionado));
                    if (value != null)
                        _ = CargarConversacionAsync(value.CredencialId);
                }
            }
        }

        public bool HayUsuarioSeleccionado => UsuarioSeleccionado != null;

        private string _textoMensaje = string.Empty;
        public string TextoMensaje
        {
            get => _textoMensaje;
            set => SetProperty(ref _textoMensaje, value);
        }

        private bool _estaConectado;
        public bool EstaConectado
        {
            get => _estaConectado;
            set => SetProperty(ref _estaConectado, value);
        }

        private bool _cargando;
        public bool Cargando
        {
            get => _cargando;
            set => SetProperty(ref _cargando, value);
        }

        private string _estadoConexion = "Desconectado";
        public string EstadoConexion
        {
            get => _estadoConexion;
            set => SetProperty(ref _estadoConexion, value);
        }

        private bool _enviandoImagen;
        public bool EnviandoImagen
        {
            get => _enviandoImagen;
            set => SetProperty(ref _enviandoImagen, value);
        }

        public MensajesViewModel(
            IMensajeriaService mensajeria,
            IUserSessionService session,
            IAuthService authService,
            ILoggingService logger,
            IApiEndpointProvider endpoints)
        {
            _mensajeria = mensajeria;
            _session = session;
            _authService = authService;
            _logger = logger;
            _endpoints = endpoints;
        }

        public void SetDispatcher(DispatcherQueue dispatcher) => _dispatcher = dispatcher;

        public async Task InitializeAsync()
        {
            Cargando = true;
            try
            {
                // Suscribirse a eventos de SignalR
                _mensajeria.MensajeRecibido += OnMensajeRecibido;
                _mensajeria.MensajeEnviado += OnMensajeEnviado;
                _mensajeria.MensajeLeido += OnMensajeLeido;
                _mensajeria.UsuarioConectado += OnUsuarioConectado;
                _mensajeria.UsuarioDesconectado += OnUsuarioDesconectado;
                _mensajeria.UsuarioEscribiendo += OnUsuarioEscribiendo;
                _mensajeria.EstadoConexionCambiado += OnEstadoConexionCambiado;

                // Conectar al hub
                var token = await _authService.GetAccessTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    await _mensajeria.ConectarAsync(token);
                    EstaConectado = _mensajeria.EstaConectado;
                    EstadoConexion = EstaConectado ? "Conectado" : "Error de conexión";
                }

                // Cargar usuarios
                var usuarios = await _mensajeria.GetUsuariosChatAsync();
                var miId = _session.CredencialId;
                foreach (var u in usuarios.Where(u => u.CredencialId != miId))
                    Usuarios.Add(u);

                // Marcar usuarios en línea
                if (EstaConectado)
                {
                    var enLinea = await _mensajeria.ObtenerUsuariosEnLineaAsync();
                    foreach (var id in enLinea)
                    {
                        var user = Usuarios.FirstOrDefault(u => u.CredencialId.ToString() == id);
                        if (user != null) user.EstaEnLinea = true;
                    }
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al inicializar mensajería", ex, "MensajesViewModel", "InitializeAsync");
            }
            finally
            {
                Cargando = false;
            }
        }

        public async Task CargarConversacionAsync(long otroUsuarioId)
        {
            Cargando = true;
            try
            {
                Mensajes.Clear();
                var mensajes = await _mensajeria.GetConversacionAsync(_session.CredencialId, otroUsuarioId);
                foreach (var m in mensajes)
                {
                    m.EsMio = m.DeCredencialId == _session.CredencialId;
                    m.ImagenUrlCompleta = BuildImageUrl(m.ArchivoUrl);
                    Mensajes.Add(m);
                }

                // Marcar como leídos los mensajes que me enviaron
                foreach (var m in mensajes.Where(m => !m.EsMio && !m.EsLeido))
                {
                    try { await _mensajeria.MarcarLeidoAsync(m.Id); m.LeidoEn = DateTime.Now; }
                    catch { /* Ignorar errores individuales */ }
                }

                // Limpiar contador de no leídos
                if (UsuarioSeleccionado != null)
                    UsuarioSeleccionado.MensajesNoLeidos = 0;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al cargar conversación", ex, "MensajesViewModel", "CargarConversacionAsync");
            }
            finally
            {
                Cargando = false;
            }
        }

        public async Task EnviarMensajeAsync()
        {
            if (UsuarioSeleccionado == null || string.IsNullOrWhiteSpace(TextoMensaje)) return;

            var texto = TextoMensaje.Trim();
            TextoMensaje = string.Empty;

            try
            {
                await _mensajeria.EnviarMensajeAsync(UsuarioSeleccionado.CredencialId, texto);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al enviar mensaje", ex, "MensajesViewModel", "EnviarMensajeAsync");
                TextoMensaje = texto; // Restaurar el texto si falla
            }
        }

        public async Task NotificarEscribiendoAsync()
        {
            if (UsuarioSeleccionado == null) return;
            try { await _mensajeria.EnviandoEscribiendoAsync(UsuarioSeleccionado.CredencialId); }
            catch { /* Ignorar */ }
        }

        /// <summary>Enviar un archivo (imagen o PDF) al chat.</summary>
        public async Task EnviarArchivoAsync(Stream fileStream, string fileName, string contentType)
        {
            if (UsuarioSeleccionado == null) return;

            EnviandoImagen = true;
            try
            {
                var archivoUrl = await _mensajeria.UploadImagenMensajeAsync(
                    _session.CredencialId, UsuarioSeleccionado.CredencialId,
                    fileStream, fileName, contentType);

                if (string.IsNullOrEmpty(archivoUrl))
                {
                    await _logger.LogErrorAsync("No se pudo subir el archivo", null, "MensajesViewModel", "EnviarArchivoAsync");
                    return;
                }

                var esPdf = fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
                var tipo = esPdf ? "pdf" : "imagen";
                var contenido = esPdf ? $"📄 {fileName}" : "📷 Imagen";

                await _mensajeria.EnviarMensajeAsync(
                    UsuarioSeleccionado.CredencialId,
                    contenido,
                    tipo: tipo,
                    archivoUrl: archivoUrl);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al enviar archivo", ex, "MensajesViewModel", "EnviarArchivoAsync");
            }
            finally
            {
                EnviandoImagen = false;
            }
        }

        /// <summary>Construye la URL completa de imagen a partir de la URL relativa.</summary>
        private string? BuildImageUrl(string? archivoUrl)
        {
            if (string.IsNullOrEmpty(archivoUrl)) return null;
            var baseUrl = _endpoints.GetApiBaseUrl().TrimEnd('/');
            return archivoUrl.StartsWith("http") ? archivoUrl : $"{baseUrl}{archivoUrl}";
        }

        public async Task CleanupAsync()
        {
            _mensajeria.MensajeRecibido -= OnMensajeRecibido;
            _mensajeria.MensajeEnviado -= OnMensajeEnviado;
            _mensajeria.MensajeLeido -= OnMensajeLeido;
            _mensajeria.UsuarioConectado -= OnUsuarioConectado;
            _mensajeria.UsuarioDesconectado -= OnUsuarioDesconectado;
            _mensajeria.UsuarioEscribiendo -= OnUsuarioEscribiendo;
            _mensajeria.EstadoConexionCambiado -= OnEstadoConexionCambiado;
            await _mensajeria.DesconectarAsync();
        }

        // --- Event handlers ---

        private void OnMensajeRecibido(object? sender, MensajeDto msg)
        {
            _dispatcher?.TryEnqueue(() =>
            {
                try
                {
                    msg.EsMio = false;
                    msg.ImagenUrlCompleta = BuildImageUrl(msg.ArchivoUrl);
                    if (UsuarioSeleccionado?.CredencialId == msg.DeCredencialId)
                    {
                        Mensajes.Add(msg);
                        _ = _mensajeria.MarcarLeidoAsync(msg.Id);
                    }
                    else
                    {
                        var user = Usuarios.FirstOrDefault(u => u.CredencialId == msg.DeCredencialId);
                        if (user != null) user.MensajesNoLeidos++;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en OnMensajeRecibido: {ex}");
                }
            });
        }

        private void OnMensajeEnviado(object? sender, MensajeDto msg)
        {
            _dispatcher?.TryEnqueue(() =>
            {
                try
                {
                    msg.EsMio = true;
                    msg.ImagenUrlCompleta = BuildImageUrl(msg.ArchivoUrl);
                    if (UsuarioSeleccionado?.CredencialId == msg.ParaCredencialId)
                        Mensajes.Add(msg);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en OnMensajeEnviado: {ex}");
                }
            });
        }

        private void OnMensajeLeido(object? sender, long mensajeId)
        {
            _dispatcher?.TryEnqueue(() =>
            {
                var msg = Mensajes.FirstOrDefault(m => m.Id == mensajeId);
                if (msg != null) msg.LeidoEn = DateTime.Now;
            });
        }

        private void OnUsuarioConectado(object? sender, string credencialId)
        {
            _dispatcher?.TryEnqueue(() =>
            {
                var user = Usuarios.FirstOrDefault(u => u.CredencialId.ToString() == credencialId);
                if (user != null) user.EstaEnLinea = true;
            });
        }

        private void OnUsuarioDesconectado(object? sender, string credencialId)
        {
            _dispatcher?.TryEnqueue(() =>
            {
                var user = Usuarios.FirstOrDefault(u => u.CredencialId.ToString() == credencialId);
                if (user != null) { user.EstaEnLinea = false; user.EstaEscribiendo = false; }
            });
        }

        private void OnUsuarioEscribiendo(object? sender, string credencialId)
        {
            _dispatcher?.TryEnqueue(async () =>
            {
                var user = Usuarios.FirstOrDefault(u => u.CredencialId.ToString() == credencialId);
                if (user != null)
                {
                    user.EstaEscribiendo = true;
                    // Auto-limpiar después de 3 segundos
                    await Task.Delay(3000);
                    user.EstaEscribiendo = false;
                }
            });
        }

        private void OnEstadoConexionCambiado(object? sender, bool conectado)
        {
            _dispatcher?.TryEnqueue(() =>
            {
                EstaConectado = conectado;
                EstadoConexion = conectado ? "Conectado" : "Reconectando…";
            });
        }
    }
}
