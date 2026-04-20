using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.Services.DevOps
{
    /// <summary>
    /// Servicio cliente para operaciones DevOps de limpieza de datos.
    /// </summary>
    public interface IDevOpsService
    {
        /// <summary>Limpia datos financieros</summary>
        Task<List<DevOpsWipeResult>> LimpiarFinancieroAsync(CancellationToken ct = default);

        /// <summary>Limpia datos de operaciones</summary>
        Task<List<DevOpsWipeResult>> LimpiarOperacionesAsync(CancellationToken ct = default);

        /// <summary>Limpia datos de mantenimiento</summary>
        Task<List<DevOpsWipeResult>> LimpiarMantenimientoAsync(CancellationToken ct = default);

        /// <summary>Limpia datos de levantamientos</summary>
        Task<List<DevOpsWipeResult>> LimpiarLevantamientosAsync(CancellationToken ct = default);

        /// <summary>Limpia datos de servicios</summary>
        Task<List<DevOpsWipeResult>> LimpiarServiciosAsync(CancellationToken ct = default);

        /// <summary>Limpia logs y actividades</summary>
        Task<List<DevOpsWipeResult>> LimpiarLogsAsync(CancellationToken ct = default);

        /// <summary>Limpia areas, coordenadas, marcadores, ubicaciones y relaciones usuario-area</summary>
        Task<List<DevOpsWipeResult>> LimpiarUbicacionesAsync(CancellationToken ct = default);

        /// <summary>Borra todos los permisos de módulo y acción, reinicia secuencias</summary>
        Task<List<DevOpsWipeResult>> LimpiarPermisosAsync(CancellationToken ct = default);

        /// <summary>Borra facturas, estados de cuenta y todo lo vinculado dentro del rango de fechas</summary>
        Task<List<DevOpsWipeResult>> LimpiarConciliacionPorRangoAsync(DateTime fechaInicio, DateTime fechaFin, CancellationToken ct = default);

        /// <summary>Obtiene estadísticas de la base de datos</summary>
        Task<List<DevOpsStatsResult>> ObtenerEstadisticasAsync(CancellationToken ct = default);

        /// <summary>Envía un mensaje de prueba con emisor arbitrario para testing</summary>
        Task EnviarMensajePruebaAsync(long deCredencialId, long paraCredencialId, string contenido, CancellationToken ct = default);
    }

    /// <summary>Resultado de limpieza por tabla</summary>
    public class DevOpsWipeResult
    {
        public string Tabla { get; set; } = string.Empty;
        public long RegistrosEliminados { get; set; }
    }

    /// <summary>Estadísticas por tabla</summary>
    public class DevOpsStatsResult
    {
        public string Tabla { get; set; } = string.Empty;
        public long TotalRegistros { get; set; }
    }
}
