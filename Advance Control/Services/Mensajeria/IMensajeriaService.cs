using Advance_Control.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.Services.Mensajeria
{
    public interface IMensajeriaService
    {
        /// <summary>Indica si la conexión SignalR está activa.</summary>
        bool EstaConectado { get; }

        /// <summary>
        /// ID del usuario cuya conversación está visible en pantalla.
        /// Se usa para suprimir notificaciones de mensajes de este usuario.
        /// </summary>
        long? UsuarioVisibleId { get; set; }

        /// <summary>Se dispara cuando se recibe un mensaje nuevo.</summary>
        event EventHandler<MensajeDto>? MensajeRecibido;

        /// <summary>Se dispara cuando un mensaje enviado es confirmado por el servidor.</summary>
        event EventHandler<MensajeDto>? MensajeEnviado;

        /// <summary>Se dispara cuando un mensaje es marcado como leído por el destinatario.</summary>
        event EventHandler<long>? MensajeLeido;

        /// <summary>Se dispara cuando un usuario se conecta.</summary>
        event EventHandler<string>? UsuarioConectado;

        /// <summary>Se dispara cuando un usuario se desconecta.</summary>
        event EventHandler<string>? UsuarioDesconectado;

        /// <summary>Se dispara cuando un usuario está escribiendo.</summary>
        event EventHandler<string>? UsuarioEscribiendo;

        /// <summary>Se dispara cuando cambia el estado de conexión.</summary>
        event EventHandler<bool>? EstadoConexionCambiado;

        /// <summary>Conectar al hub de SignalR.</summary>
        Task ConectarAsync(string token, CancellationToken ct = default);

        /// <summary>Desconectar del hub.</summary>
        Task DesconectarAsync();

        /// <summary>Enviar un mensaje via SignalR.</summary>
        Task EnviarMensajeAsync(long paraCredencialId, string contenido, string tipo = "mensaje",
            int? idReferencia = null, string? tipoReferencia = null, string? fechaLimite = null,
            string? archivoUrl = null);

        /// <summary>Marcar un mensaje como leído via SignalR.</summary>
        Task MarcarLeidoAsync(long mensajeId);

        /// <summary>Notificar que estoy escribiendo.</summary>
        Task EnviandoEscribiendoAsync(long paraCredencialId);

        /// <summary>Obtener los IDs de usuarios en línea.</summary>
        Task<List<string>> ObtenerUsuariosEnLineaAsync();

        // --- Métodos REST (para historial y datos) ---

        /// <summary>Obtener la conversación completa entre dos usuarios via REST.</summary>
        Task<List<MensajeDto>> GetConversacionAsync(long usuario1, long usuario2, CancellationToken ct = default);

        /// <summary>Obtener usuarios disponibles para chat via REST.</summary>
        Task<List<UsuarioChatDto>> GetUsuariosChatAsync(CancellationToken ct = default);

        /// <summary>Obtener conteo de mensajes no leídos via REST.</summary>
        Task<long> GetNoLeidosCountAsync(long credencialId, CancellationToken ct = default);

        /// <summary>Subir una imagen al servidor y retornar la URL.</summary>
        Task<string?> UploadImagenMensajeAsync(long deCredencialId, long paraCredencialId,
            Stream imageStream, string fileName, string contentType, CancellationToken ct = default);
    }
}
