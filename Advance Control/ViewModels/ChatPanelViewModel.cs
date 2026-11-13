using Advance_Control.Models;
using Advance_Control.Services.Auth;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Mensajeria;
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.Session;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// ViewModel Singleton para el panel lateral de chat simplificado.
    /// Funciona independientemente de MensajesViewModel.
    /// </summary>
    public class ChatPanelViewModel : ViewModelBase
    {
        private readonly IMensajeriaService _mensajeria;
        private readonly IUserSessionService _session;
        private readonly IAuthService _authService;
        private readonly ILoggingService _logger;
        private readonly IApiEndpointProvider _endpoints;
        private readonly INotificacionService _notificacionService;
        private readonly Services.Http.IArchivoProtegidoService _archivos;
        private DispatcherQueue? _dispatcher;
        private bool _eventosSubscritos;

        /// <summary>
        /// ID de credencial del último mensaje recibido que generó notificación.
        /// Se usa como fallback al hacer clic en la notificación, en caso de que
        /// los argumentos de la notificación no se parseen correctamente.
        /// </summary>
        public static long UltimaNotificacionCredencialId { get; set; }

        public ObservableCollection<UsuarioChatDto> Usuarios { get; } = new();
        public ObservableCollection<MensajeDto> Mensajes { get; } = new();

        /// <summary>Chats existentes (personas con mensajes), el más reciente arriba.</summary>
        public ObservableCollection<UsuarioChatDto> Chats { get; } = new();

        private bool _hayChats;
        public bool HayChats
        {
            get => _hayChats;
            set { if (SetProperty(ref _hayChats, value)) OnPropertyChanged(nameof(SinChats)); }
        }

        private bool _cargandoChats;
        public bool CargandoChats
        {
            get => _cargandoChats;
            set { if (SetProperty(ref _cargandoChats, value)) OnPropertyChanged(nameof(SinChats)); }
        }

        /// <summary>Mensaje "aún no tienes chats".</summary>
        public bool SinChats => !HayChats && !CargandoChats;

        private readonly List<UsuarioChatDto> _todosLosUsuarios = new();

        private UsuarioChatDto? _usuarioSeleccionado;
        public UsuarioChatDto? UsuarioSeleccionado
        {
            get => _usuarioSeleccionado;
            set
            {
                if (SetProperty(ref _usuarioSeleccionado, value))
                {
                    OnPropertyChanged(nameof(HayUsuarioSeleccionado));
                    OnPropertyChanged(nameof(MostrarBarraEntrada));
                    OnPropertyChanged(nameof(MostrarPreviewRespuesta));
                    OnPropertyChanged(nameof(RespuestaAutor));
                    MensajeRespondiendo = null;
                    if (IsPanelVisible)
                        _mensajeria.UsuarioVisibleId = value?.CredencialId;
                    if (value != null)
                        IsUserListVisible = false;
                    if (value != null)
                        _ = CargarConversacionSeguroAsync(value.CredencialId);
                }
            }
        }

        public bool HayUsuarioSeleccionado => UsuarioSeleccionado != null;

        public bool MostrarBarraEntrada => HayUsuarioSeleccionado && !IsUserListVisible;

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

        private bool _isUserListVisible = false;
        public bool IsUserListVisible
        {
            get => _isUserListVisible;
            set
            {
                if (SetProperty(ref _isUserListVisible, value))
                {
                    OnPropertyChanged(nameof(MostrarBarraEntrada));
                    OnPropertyChanged(nameof(MostrarPreviewRespuesta));
                }
            }
        }

        private bool _isPanelVisible;
        public bool IsPanelVisible
        {
            get => _isPanelVisible;
            set
            {
                if (SetProperty(ref _isPanelVisible, value))
                {
                    if (!value)
                    {
                        // Al ocultar el panel, limpiar usuario visible
                        _mensajeria.UsuarioVisibleId = null;
                    }
                    else if (UsuarioSeleccionado != null)
                    {
                        _mensajeria.UsuarioVisibleId = UsuarioSeleccionado.CredencialId;
                    }
                }
            }
        }

        private string _errorMensaje = string.Empty;
        public string ErrorMensaje
        {
            get => _errorMensaje;
            set => SetProperty(ref _errorMensaje, value);
        }

        private MensajeDto? _mensajeRespondiendo;
        /// <summary>Mensaje al que se está respondiendo (estilo WhatsApp). Null si no hay respuesta activa.</summary>
        public MensajeDto? MensajeRespondiendo
        {
            get => _mensajeRespondiendo;
            set
            {
                if (SetProperty(ref _mensajeRespondiendo, value))
                {
                    OnPropertyChanged(nameof(HayRespuestaActiva));
                    OnPropertyChanged(nameof(MostrarPreviewRespuesta));
                    OnPropertyChanged(nameof(RespuestaResumen));
                    OnPropertyChanged(nameof(RespuestaAutor));
                }
            }
        }

        public bool HayRespuestaActiva => MensajeRespondiendo != null;
        public bool MostrarPreviewRespuesta => HayRespuestaActiva && MostrarBarraEntrada;
        public string RespuestaAutor => MensajeRespondiendo?.EsMio == true ? "Tú" : (UsuarioSeleccionado?.NombreVisible ?? string.Empty);
        public string RespuestaResumen
        {
            get
            {
                var c = MensajeRespondiendo?.Contenido ?? string.Empty;
                if (c.Length > 80) c = c.Substring(0, 80) + "…";
                return c;
            }
        }

        public ChatPanelViewModel(
            IMensajeriaService mensajeria,
            IUserSessionService session,
            IAuthService authService,
            ILoggingService logger,
            IApiEndpointProvider endpoints,
            INotificacionService notificacionService,
            Services.Http.IArchivoProtegidoService archivos)
        {
            _archivos = archivos ?? throw new ArgumentNullException(nameof(archivos));
            _mensajeria = mensajeria ?? throw new ArgumentNullException(nameof(mensajeria));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _notificacionService = notificacionService ?? throw new ArgumentNullException(nameof(notificacionService));
        }

        public void SetDispatcher(DispatcherQueue dispatcher) => _dispatcher = dispatcher;

        /// <summary>
        /// Inicializa el panel: suscribe eventos SignalR y carga usuarios.
        /// Se llama cuando el panel se muestra por primera vez.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_eventosSubscritos) return;

            Cargando = true;
            try
            {
                _mensajeria.MensajeRecibido += OnMensajeRecibido;
                _mensajeria.MensajeEnviado += OnMensajeEnviado;
                _mensajeria.MensajeLeido += OnMensajeLeido;
                _mensajeria.MensajesLeidos += OnMensajesLeidos;
                _mensajeria.MensajesEntregados += OnMensajesEntregados;
                _mensajeria.MensajeEliminado += OnMensajeEliminado;
                _mensajeria.MensajeOcultado += OnMensajeOcultado;
                _mensajeria.UsuarioConectado += OnUsuarioConectado;
                _mensajeria.UsuarioDesconectado += OnUsuarioDesconectado;
                _mensajeria.UsuarioEscribiendo += OnUsuarioEscribiendo;
                _mensajeria.EstadoConexionCambiado += OnEstadoConexionCambiado;
                _eventosSubscritos = true;

                EstaConectado = _mensajeria.EstaConectado;
                EstadoConexion = EstaConectado ? "Conectado" : "Sin conexión";

                await CargarUsuariosAsync();
                await CargarChatsAsync();
            }
            catch (Exception ex)
            {
                try { await _logger.LogErrorAsync("Error al inicializar chat panel", ex, "ChatPanelViewModel", "InitializeAsync"); } catch { }
            }
            finally
            {
                Cargando = false;
            }
        }

        private async Task CargarUsuariosAsync()
        {
            try
            {
                var usuarios = await _mensajeria.GetUsuariosChatAsync();
                var miId = _session.CredencialId;
                _todosLosUsuarios.Clear();
                Usuarios.Clear();

                foreach (var u in usuarios.Where(u => u.CredencialId != miId))
                {
                    _todosLosUsuarios.Add(u);
                    Usuarios.Add(u);
                }

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
                try { await _logger.LogErrorAsync("Error al cargar usuarios del chat", ex, "ChatPanelViewModel", "CargarUsuariosAsync"); } catch { }
            }
        }

        /// <summary>
        /// Carga los chats existentes con su último mensaje y no leídos. Usa las mismas instancias
        /// de usuario que la lista de contactos (en línea / escribiendo / no leídos quedan sincronizados).
        /// </summary>
        public async Task CargarChatsAsync()
        {
            if (CargandoChats) return;
            CargandoChats = true;
            try
            {
                if (_todosLosUsuarios.Count == 0)
                    await CargarUsuariosAsync();

                var chats = await _mensajeria.GetConversacionesAsync();
                Chats.Clear();
                foreach (var c in chats)
                {
                    var usuario = _todosLosUsuarios.FirstOrDefault(u => u.CredencialId == c.OtroCredencialId);
                    if (usuario == null) continue; // usuario inactivo o sin acceso al chat
                    usuario.UltimoMensaje = ConversacionResumenDto.Resumen(c.UltimoContenido, c.UltimoEliminado, c.UltimoEsMio);
                    usuario.UltimoEn = ConversacionResumenDto.ALocal(c.UltimoEn);
                    if (UsuarioSeleccionado?.CredencialId != usuario.CredencialId)
                        usuario.MensajesNoLeidos = (int)c.NoLeidos;
                    Chats.Add(usuario);
                }
                HayChats = Chats.Count > 0;
            }
            catch (Exception ex)
            {
                try { await _logger.LogErrorAsync("Error al cargar los chats", ex, "ChatPanelViewModel", "CargarChatsAsync"); } catch { }
            }
            finally
            {
                CargandoChats = false;
            }
        }

        /// <summary>Regresa a la lista de chats (al abrir la cortina o con la flecha de la conversación).</summary>
        public void MostrarListaChats()
        {
            UsuarioSeleccionado = null;
            IsUserListVisible = false;
            _ = CargarChatsAsync();
        }

        /// <summary>Sube el chat con esa persona al inicio y actualiza su último mensaje.</summary>
        private void ActualizarChat(long otroCredencialId, MensajeDto msg, bool esMio)
        {
            var usuario = _todosLosUsuarios.FirstOrDefault(u => u.CredencialId == otroCredencialId);
            if (usuario == null) return;
            usuario.UltimoMensaje = ConversacionResumenDto.Resumen(msg.Contenido, msg.EstaEliminado, esMio);
            usuario.UltimoEn = msg.CreatedAt == default ? DateTime.Now : ConversacionResumenDto.ALocal(msg.CreatedAt);
            var idx = Chats.IndexOf(usuario);
            if (idx < 0) Chats.Insert(0, usuario);
            else if (idx > 0) Chats.Move(idx, 0);
            HayChats = true;
        }

        /// <summary>Tras reconectar: chats, en línea y la conversación abierta pudieron cambiar.</summary>
        private async Task RefrescarTrasReconexionAsync()
        {
            try
            {
                await CargarChatsAsync();
                var enLinea = (await _mensajeria.ObtenerUsuariosEnLineaAsync()).ToHashSet();
                foreach (var u in _todosLosUsuarios)
                    u.EstaEnLinea = enLinea.Contains(u.CredencialId.ToString());
                if (UsuarioSeleccionado != null)
                    await CargarConversacionSeguroAsync(UsuarioSeleccionado.CredencialId);
            }
            catch (Exception ex)
            {
                try { await _logger.LogErrorAsync("Error al refrescar el chat tras reconectar", ex, "ChatPanelViewModel", "RefrescarTrasReconexionAsync"); } catch { }
            }
        }

        /// <summary>
        /// Abre la conversación con un usuario específico.
        /// Puede ser llamado externamente (ej. desde MensajesPage).
        /// </summary>
        public void AbrirConversacion(UsuarioChatDto usuario)
        {
            UsuarioSeleccionado = usuario;
            IsUserListVisible = false;
        }

        /// <summary>
        /// Abre la conversación buscando por CredencialId.
        /// Usado al hacer clic en una notificación de mensaje.
        /// </summary>
        public void AbrirConversacionPorId(long credencialId)
        {
            var usuario = _todosLosUsuarios.FirstOrDefault(u => u.CredencialId == credencialId);
            System.Diagnostics.Debug.WriteLine($"[ChatVM] AbrirConversacionPorId({credencialId}), encontrado={usuario != null}, Usuarios.Count={Usuarios.Count}");
            if (usuario != null)
                AbrirConversacion(usuario);
        }

        private async Task CargarConversacionSeguroAsync(long otroUsuarioId)
        {
            try
            {
                await CargarConversacionAsync(otroUsuarioId);
            }
            catch (Exception ex)
            {
                ErrorMensaje = $"Error: {ex.GetType().Name}: {ex.Message}";
                try { await _logger.LogErrorAsync("Error al cargar conversación", ex, "ChatPanelViewModel", "CargarConversacionSeguroAsync"); } catch { }
            }
        }

        private async Task CargarConversacionAsync(long otroUsuarioId)
        {
            Cargando = true;
            try
            {
                Mensajes.Clear();
                var mensajes = await _mensajeria.GetConversacionAsync(_session.CredencialId, otroUsuarioId);
                foreach (var m in mensajes)
                {
                    m.EsMio = m.DeCredencialId == _session.CredencialId;
                    m.ImagenUrlCompleta = BuildImageUrl(m);
                    Mensajes.Add(m);
                }

                // Bulk-marcar como leído (1 sola llamada al hub en lugar de N).
                // El servidor responderá con 'MensajesLeidos' al remitente; aquí asumimos OK localmente.
                try
                {
                    if (UsuarioSeleccionado != null)
                        await _mensajeria.MarcarConversacionLeidaAsync(UsuarioSeleccionado.CredencialId);
                }
                catch { }
                var ahora = DateTime.UtcNow;
                foreach (var m in mensajes.Where(m => !m.EsMio && !m.EsLeido))
                {
                    m.LeidoEn = ahora;
                }

                if (UsuarioSeleccionado != null)
                    UsuarioSeleccionado.MensajesNoLeidos = 0;
            }
            catch (Exception ex)
            {
                try { await _logger.LogErrorAsync("Error al cargar conversación", ex, "ChatPanelViewModel", "CargarConversacionAsync"); } catch { }
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
            var respuestaA = MensajeRespondiendo?.Id;
            MensajeRespondiendo = null;

            try
            {
                await _mensajeria.EnviarMensajeAsync(UsuarioSeleccionado.CredencialId, texto,
                    respuestaAMensajeId: respuestaA);
            }
            catch (Exception ex)
            {
                ErrorMensaje = $"Error envío: {ex.GetType().Name}: {ex.Message}";
                try { await _logger.LogErrorAsync("Error al enviar mensaje", ex, "ChatPanelViewModel", "EnviarMensajeAsync"); } catch { }
                TextoMensaje = texto;
            }
        }

        /// <summary>Inicia el modo respuesta para un mensaje (estilo WhatsApp).</summary>
        public void IniciarRespuesta(MensajeDto mensaje)
        {
            if (mensaje == null || mensaje.EstaEliminado) return;
            MensajeRespondiendo = mensaje;
        }

        /// <summary>Cancela el modo respuesta.</summary>
        public void CancelarRespuesta() => MensajeRespondiendo = null;

        /// <summary>Elimina un mensaje propio para todos (soft-delete global).</summary>
        public async Task EliminarMensajeParaTodosAsync(MensajeDto mensaje)
        {
            if (mensaje == null || !mensaje.EsMio) return;
            try { await _mensajeria.EliminarMensajeParaTodosAsync(mensaje.Id); }
            catch (Exception ex)
            {
                ErrorMensaje = $"Error al eliminar: {ex.Message}";
                try { await _logger.LogErrorAsync("Error al eliminar mensaje para todos", ex, "ChatPanelViewModel", "EliminarMensajeParaTodosAsync"); } catch { }
            }
        }

        /// <summary>Oculta un mensaje únicamente para el usuario actual.</summary>
        public async Task OcultarMensajeParaMiAsync(MensajeDto mensaje)
        {
            if (mensaje == null) return;
            try { await _mensajeria.OcultarMensajeParaMiAsync(mensaje.Id); }
            catch (Exception ex)
            {
                ErrorMensaje = $"Error al ocultar: {ex.Message}";
                try { await _logger.LogErrorAsync("Error al ocultar mensaje", ex, "ChatPanelViewModel", "OcultarMensajeParaMiAsync"); } catch { }
            }
        }

        /// <summary>TODO: integrar con módulo de tareas.</summary>
        public Task CrearTareaDesdeMensajeAsync(MensajeDto mensaje)
        {
            _ = _logger.LogInformationAsync($"Crear tarea solicitado para mensaje {mensaje?.Id}", "ChatPanelViewModel", "CrearTareaDesdeMensajeAsync");
            return Task.CompletedTask;
        }

        public async Task NotificarEscribiendoAsync()
        {
            if (UsuarioSeleccionado == null) return;
            try { await _mensajeria.EnviandoEscribiendoAsync(UsuarioSeleccionado.CredencialId); }
            catch { }
        }

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
                    await _logger.LogErrorAsync("No se pudo subir el archivo", null, "ChatPanelViewModel", "EnviarArchivoAsync");
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
                try { await _logger.LogErrorAsync("Error al enviar archivo", ex, "ChatPanelViewModel", "EnviarArchivoAsync"); } catch { }
            }
            finally
            {
                EnviandoImagen = false;
            }
        }

        public void FiltrarUsuarios(string texto)
        {
            Usuarios.Clear();
            var filtro = texto?.Trim().ToLowerInvariant() ?? string.Empty;
            var resultados = string.IsNullOrEmpty(filtro)
                ? _todosLosUsuarios
                : _todosLosUsuarios.Where(u =>
                    (u.NombreVisible?.ToLowerInvariant().Contains(filtro) ?? false) ||
                    (u.Usuario?.ToLowerInvariant().Contains(filtro) ?? false));

            foreach (var u in resultados)
                Usuarios.Add(u);
        }

        /// <summary>
        /// URL absoluta del archivo (sin duplicar "/pruebas"). Si es imagen, la baja con token
        /// en segundo plano y la muestra al terminar.
        /// </summary>
        private string? BuildImageUrl(MensajeDto mensaje)
        {
            var url = _archivos.ResolverUrl(mensaje.ArchivoUrl);
            if (url != null && mensaje.EsImagen)
                _ = CargarImagenAsync(mensaje, url);
            return url;
        }

        private async Task CargarImagenAsync(MensajeDto mensaje, string url)
        {
            var local = await _archivos.DescargarAsync(url);
            if (local == null) return;
            if (_dispatcher != null && !_dispatcher.HasThreadAccess)
                _dispatcher.TryEnqueue(() => mensaje.MostrarImagenLocal(local));
            else
                mensaje.MostrarImagenLocal(local);
        }

        /// <summary>Abre un PDF (u otro archivo) del chat: lo baja con token y lo abre con Windows.</summary>
        public Task<bool> AbrirArchivoAsync(string? url) => _archivos.AbrirAsync(url);

        // --- Event handlers ---

        private void OnMensajeRecibido(object? sender, MensajeDto msg)
        {
            _dispatcher?.TryEnqueue(async () =>
            {
                try
                {
                    msg.EsMio = false;
                    msg.ImagenUrlCompleta = BuildImageUrl(msg);

                    if (UsuarioSeleccionado?.CredencialId == msg.DeCredencialId)
                    {
                        Mensajes.Add(msg);
                        _ = _mensajeria.MarcarLeidoAsync(msg.Id);
                    }
                    else
                    {
                        var user = _todosLosUsuarios.FirstOrDefault(u => u.CredencialId == msg.DeCredencialId);
                        if (user != null) user.MensajesNoLeidos++;
                    }
                    ActualizarChat(msg.DeCredencialId, msg, esMio: false);

                    // Notificación si el remitente no es el usuario visible
                    if (msg.DeCredencialId != _mensajeria.UsuarioVisibleId)
                    {
                        UltimaNotificacionCredencialId = msg.DeCredencialId;
                        var remitente = _todosLosUsuarios.FirstOrDefault(u => u.CredencialId == msg.DeCredencialId);
                        var nombre = remitente?.NombreVisible ?? "Nuevo mensaje";
                        try
                        {
                            await _notificacionService.MostrarNotificacionAsync(
                                nombre,
                                msg.ContenidoSeguro,
                                tiempoDeVidaSegundos: 5,
                                argumentos: new System.Collections.Generic.Dictionary<string, string>
                                {
                                    { "accion", "abrirChat" },
                                    { "credencialId", msg.DeCredencialId.ToString() }
                                });
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en ChatPanel.OnMensajeRecibido: {ex}");
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
                    msg.ImagenUrlCompleta = BuildImageUrl(msg);
                    if (UsuarioSeleccionado?.CredencialId == msg.ParaCredencialId)
                        Mensajes.Add(msg);
                    ActualizarChat(msg.ParaCredencialId, msg, esMio: true);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en ChatPanel.OnMensajeEnviado: {ex}");
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

        private void OnMensajesLeidos(object? sender, List<long> ids)
        {
            _dispatcher?.TryEnqueue(() =>
            {
                var ahora = DateTime.UtcNow;
                var set = new HashSet<long>(ids);
                foreach (var msg in Mensajes)
                {
                    if (set.Contains(msg.Id) && !msg.EsLeido)
                        msg.LeidoEn = ahora;
                }
            });
        }

        private void OnMensajesEntregados(object? sender, List<long> ids)
        {
            _dispatcher?.TryEnqueue(() =>
            {
                var ahora = DateTime.UtcNow;
                var set = new HashSet<long>(ids);
                foreach (var msg in Mensajes)
                {
                    if (set.Contains(msg.Id) && !msg.EsEntregado)
                        msg.EntregadoEn = ahora;
                }
            });
        }

        private void OnMensajeEliminado(object? sender, long mensajeId)
        {
            _dispatcher?.TryEnqueue(() =>
            {
                var msg = Mensajes.FirstOrDefault(m => m.Id == mensajeId);
                if (msg != null) msg.EliminadoEn = DateTime.Now;
                if (MensajeRespondiendo?.Id == mensajeId) MensajeRespondiendo = null;
            });
        }

        private void OnMensajeOcultado(object? sender, long mensajeId)
        {
            _dispatcher?.TryEnqueue(() =>
            {
                var msg = Mensajes.FirstOrDefault(m => m.Id == mensajeId);
                if (msg != null) Mensajes.Remove(msg);
                if (MensajeRespondiendo?.Id == mensajeId) MensajeRespondiendo = null;
            });
        }

        private void OnUsuarioConectado(object? sender, string credencialId)
        {
            _dispatcher?.TryEnqueue(() =>
            {
                var user = _todosLosUsuarios.FirstOrDefault(u => u.CredencialId.ToString() == credencialId);
                if (user != null) user.EstaEnLinea = true;
            });
        }

        private void OnUsuarioDesconectado(object? sender, string credencialId)
        {
            _dispatcher?.TryEnqueue(() =>
            {
                var user = _todosLosUsuarios.FirstOrDefault(u => u.CredencialId.ToString() == credencialId);
                if (user != null) { user.EstaEnLinea = false; user.EstaEscribiendo = false; }
            });
        }

        private void OnUsuarioEscribiendo(object? sender, string credencialId)
        {
            _dispatcher?.TryEnqueue(async () =>
            {
                var user = _todosLosUsuarios.FirstOrDefault(u => u.CredencialId.ToString() == credencialId);
                if (user != null)
                {
                    user.EstaEscribiendo = true;
                    await Task.Delay(3000);
                    user.EstaEscribiendo = false;
                }
            });
        }

        private void OnEstadoConexionCambiado(object? sender, bool conectado)
        {
            _dispatcher?.TryEnqueue(() =>
            {
                var reconectado = conectado && !EstaConectado;
                EstaConectado = conectado;
                EstadoConexion = conectado ? "Conectado" : "Reconectando…";
                if (reconectado)
                    _ = RefrescarTrasReconexionAsync();
            });
        }
    }
}
