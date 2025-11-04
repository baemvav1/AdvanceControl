# REPORTE DE IMPLEMENTACIÓN - SISTEMA DE LOGGING

**Fecha:** 2025-11-04  
**Proyecto:** AdvanceControl (WinUI 3 Application)  
**Versión:** 1.0

---

## RESUMEN EJECUTIVO

Este documento describe la implementación del sistema de logging para la aplicación AdvanceControl, incluyendo:
- Servicio de logging en la aplicación cliente
- Estructura del endpoint API para recibir logs
- Esquema de la tabla en base de datos MS SQL Server
- Comportamiento del procedimiento almacenado

---

## 1. SERVICIO DE LOGGING - CLIENTE

### 1.1 Archivos Creados

**Ubicación:** `Services/Logging/`

1. **ILoggingService.cs** - Interface del servicio de logging
2. **LoggingService.cs** - Implementación del servicio

**Ubicación:** `Models/`

3. **LogEntry.cs** - Modelo de datos para entradas de log

### 1.2 Funcionalidades del Servicio

El servicio proporciona métodos para registrar logs en diferentes niveles:

```csharp
- LogTraceAsync() - Logs de nivel Trace
- LogDebugAsync() - Logs de nivel Debug
- LogInformationAsync() - Logs informativos
- LogWarningAsync() - Advertencias
- LogErrorAsync() - Errores con excepción opcional
- LogCriticalAsync() - Errores críticos con excepción opcional
- LogAsync() - Método genérico para logs personalizados
```

### 1.3 Características

- **Fire-and-Forget**: Los logs se envían de forma asíncrona sin bloquear la aplicación
- **Timeout**: 5 segundos máximo para envío de logs
- **Resiliente**: Los errores en el logging no afectan la aplicación principal
- **Automático**: Captura información de máquina, versión de app y timestamp automáticamente

---

## 2. ESTRUCTURA DEL ENDPOINT API

### 2.1 Especificación del Endpoint

**URL:** `/api/Logging/log`  
**Método:** `POST`  
**Content-Type:** `application/json`  
**Autenticación:** Bearer Token (opcional, según configuración)

### 2.2 Estructura del Request Body (JSON)

```json
{
  "id": "string (GUID)",
  "level": "integer (0-5)",
  "message": "string",
  "exception": "string | null",
  "stackTrace": "string | null",
  "source": "string | null",
  "method": "string | null",
  "username": "string | null",
  "machineName": "string",
  "appVersion": "string",
  "timestamp": "datetime (ISO 8601 UTC)",
  "additionalData": "string (JSON) | null"
}
```

### 2.3 Niveles de Log (level)

```
0 = Trace
1 = Debug
2 = Information
3 = Warning
4 = Error
5 = Critical
```

### 2.4 Ejemplo de Request

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "level": 4,
  "message": "Error al autenticar usuario",
  "exception": "UnauthorizedAccessException: Invalid credentials",
  "stackTrace": "   at AuthService.AuthenticateAsync() in C:\\...\\AuthService.cs:line 66",
  "source": "AuthService",
  "method": "AuthenticateAsync",
  "username": "admin@example.com",
  "machineName": "DESKTOP-ABC123",
  "appVersion": "1.0.0",
  "timestamp": "2025-11-04T06:47:29.546Z",
  "additionalData": "{\"attemptNumber\": 3}"
}
```

### 2.5 Respuestas del Endpoint

#### Respuesta Exitosa (200 OK)
```json
{
  "success": true,
  "message": "Log registrado correctamente",
  "logId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

#### Error de Validación (400 Bad Request)
```json
{
  "success": false,
  "message": "Datos de log inválidos",
  "errors": [
    "El campo 'message' es requerido",
    "El campo 'level' debe estar entre 0 y 5"
  ]
}
```

#### Error del Servidor (500 Internal Server Error)
```json
{
  "success": false,
  "message": "Error al procesar el log"
}
```

---

## 3. CONTROLADOR API - IMPLEMENTACIÓN SUGERIDA

### 3.1 Estructura del Controlador (ASP.NET Core)

```csharp
[ApiController]
[Route("api/[controller]")]
public class LoggingController : ControllerBase
{
    private readonly IConfiguration _configuration;
    
    public LoggingController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("log")]
    public async Task<IActionResult> Log([FromBody] LogEntryDto logEntry)
    {
        if (logEntry == null)
            return BadRequest(new { success = false, message = "Log entry is required" });

        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid log data", errors = ModelState.Values });

        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_InsertLog", connection);
            command.CommandType = CommandType.StoredProcedure;

            // Parámetros del procedimiento almacenado
            command.Parameters.AddWithValue("@Id", logEntry.Id ?? Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("@Level", logEntry.Level);
            command.Parameters.AddWithValue("@Message", logEntry.Message ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Exception", logEntry.Exception ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@StackTrace", logEntry.StackTrace ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Source", logEntry.Source ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Method", logEntry.Method ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Username", logEntry.Username ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@MachineName", logEntry.MachineName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@AppVersion", logEntry.AppVersion ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Timestamp", logEntry.Timestamp);
            command.Parameters.AddWithValue("@AdditionalData", logEntry.AdditionalData ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();

            return Ok(new { success = true, message = "Log registrado correctamente", logId = logEntry.Id });
        }
        catch (Exception ex)
        {
            // Log el error internamente pero no exponer detalles al cliente
            return StatusCode(500, new { success = false, message = "Error al procesar el log" });
        }
    }
}
```

---

## 4. ESQUEMA DE LA BASE DE DATOS

### 4.1 Tabla: ApplicationLogs

```sql
CREATE TABLE [dbo].[ApplicationLogs]
(
    -- Identificación
    [LogId] BIGINT IDENTITY(1,1) NOT NULL,
    [Id] NVARCHAR(50) NOT NULL,
    
    -- Información del Log
    [Level] INT NOT NULL,
    [Message] NVARCHAR(MAX) NULL,
    [Exception] NVARCHAR(MAX) NULL,
    [StackTrace] NVARCHAR(MAX) NULL,
    
    -- Contexto
    [Source] NVARCHAR(255) NULL,
    [Method] NVARCHAR(255) NULL,
    [Username] NVARCHAR(255) NULL,
    
    -- Información del Cliente
    [MachineName] NVARCHAR(255) NULL,
    [AppVersion] NVARCHAR(50) NULL,
    
    -- Fecha y hora
    [Timestamp] DATETIME2(7) NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    -- Datos adicionales
    [AdditionalData] NVARCHAR(MAX) NULL,
    
    -- Constraints
    CONSTRAINT [PK_ApplicationLogs] PRIMARY KEY CLUSTERED ([LogId] ASC),
    CONSTRAINT [UK_ApplicationLogs_Id] UNIQUE NONCLUSTERED ([Id] ASC)
);
GO

-- Índices para optimizar consultas comunes
CREATE NONCLUSTERED INDEX [IX_ApplicationLogs_Level] 
    ON [dbo].[ApplicationLogs] ([Level] ASC);

CREATE NONCLUSTERED INDEX [IX_ApplicationLogs_Timestamp] 
    ON [dbo].[ApplicationLogs] ([Timestamp] DESC);

CREATE NONCLUSTERED INDEX [IX_ApplicationLogs_Username] 
    ON [dbo].[ApplicationLogs] ([Username] ASC);

CREATE NONCLUSTERED INDEX [IX_ApplicationLogs_Source] 
    ON [dbo].[ApplicationLogs] ([Source] ASC);

CREATE NONCLUSTERED INDEX [IX_ApplicationLogs_MachineName] 
    ON [dbo].[ApplicationLogs] ([MachineName] ASC);

-- Índice compuesto para consultas de análisis
CREATE NONCLUSTERED INDEX [IX_ApplicationLogs_Level_Timestamp] 
    ON [dbo].[ApplicationLogs] ([Level] ASC, [Timestamp] DESC);
GO
```

### 4.2 Descripción de Campos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `LogId` | BIGINT | Clave primaria autoincremental |
| `Id` | NVARCHAR(50) | GUID único del log (generado en cliente) |
| `Level` | INT | Nivel de severidad (0-5) |
| `Message` | NVARCHAR(MAX) | Mensaje descriptivo del log |
| `Exception` | NVARCHAR(MAX) | Mensaje de la excepción si existe |
| `StackTrace` | NVARCHAR(MAX) | Stack trace completo de la excepción |
| `Source` | NVARCHAR(255) | Nombre de la clase que generó el log |
| `Method` | NVARCHAR(255) | Nombre del método que generó el log |
| `Username` | NVARCHAR(255) | Usuario autenticado (si aplica) |
| `MachineName` | NVARCHAR(255) | Nombre de la máquina/dispositivo |
| `AppVersion` | NVARCHAR(50) | Versión de la aplicación |
| `Timestamp` | DATETIME2(7) | Fecha/hora UTC del log (cliente) |
| `CreatedAt` | DATETIME2(7) | Fecha/hora UTC de inserción (servidor) |
| `AdditionalData` | NVARCHAR(MAX) | Datos adicionales en formato JSON |

---

## 5. PROCEDIMIENTO ALMACENADO

### 5.1 sp_InsertLog

```sql
CREATE PROCEDURE [dbo].[sp_InsertLog]
    @Id NVARCHAR(50),
    @Level INT,
    @Message NVARCHAR(MAX) = NULL,
    @Exception NVARCHAR(MAX) = NULL,
    @StackTrace NVARCHAR(MAX) = NULL,
    @Source NVARCHAR(255) = NULL,
    @Method NVARCHAR(255) = NULL,
    @Username NVARCHAR(255) = NULL,
    @MachineName NVARCHAR(255) = NULL,
    @AppVersion NVARCHAR(50) = NULL,
    @Timestamp DATETIME2(7),
    @AdditionalData NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validación de nivel de log
        IF @Level < 0 OR @Level > 5
        BEGIN
            RAISERROR('El nivel de log debe estar entre 0 y 5', 16, 1);
            RETURN;
        END
        
        -- Validación de ID único
        IF EXISTS (SELECT 1 FROM ApplicationLogs WHERE Id = @Id)
        BEGIN
            -- Log duplicado, no insertar (idempotencia)
            -- Retornar sin error para evitar duplicados por retry
            COMMIT TRANSACTION;
            RETURN;
        END
        
        -- Insertar log
        INSERT INTO ApplicationLogs 
        (
            Id, Level, Message, Exception, StackTrace, 
            Source, Method, Username, MachineName, 
            AppVersion, Timestamp, AdditionalData
        )
        VALUES 
        (
            @Id, @Level, @Message, @Exception, @StackTrace, 
            @Source, @Method, @Username, @MachineName, 
            @AppVersion, @Timestamp, @AdditionalData
        );
        
        -- Si es un log crítico, ejecutar acciones adicionales
        IF @Level >= 5 -- Critical
        BEGIN
            -- Aquí se pueden agregar acciones como:
            -- - Enviar notificación a administradores
            -- - Insertar en tabla de alertas
            -- - Ejecutar procedimientos de respuesta automática
            
            -- Ejemplo: Insertar en tabla de alertas críticas
            -- INSERT INTO CriticalAlerts (LogId, AlertTimestamp, Status)
            -- VALUES (SCOPE_IDENTITY(), GETUTCDATE(), 'Pending');
        END
        
        COMMIT TRANSACTION;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        -- Re-lanzar el error para que sea manejado por el controlador
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO
```

### 5.2 Comportamiento del Procedimiento

El procedimiento almacenado `sp_InsertLog` implementa la siguiente lógica:

#### 5.2.1 Validaciones
- **Nivel de log**: Verifica que el nivel esté entre 0 y 5
- **ID único**: Verifica que no exista un log con el mismo ID (idempotencia)

#### 5.2.2 Inserción
- Inserta el log en la tabla `ApplicationLogs`
- Usa transacción para garantizar integridad
- El campo `CreatedAt` se llena automáticamente con la fecha del servidor

#### 5.2.3 Procesamiento Especial
- **Logs Críticos (Level = 5)**: 
  - Se pueden configurar acciones automáticas
  - Ejemplo: Insertar en tabla de alertas
  - Ejemplo: Enviar notificaciones a administradores
  - Ejemplo: Activar procedimientos de respuesta

#### 5.2.4 Idempotencia
- Si se recibe un log con un ID duplicado, el procedimiento:
  - No inserta el registro
  - Finaliza exitosamente (sin error)
  - Esto permite reintentos seguros desde el cliente

#### 5.2.5 Manejo de Errores
- Usa TRY-CATCH con transacciones
- En caso de error:
  - Hace ROLLBACK de la transacción
  - Re-lanza el error con información detallada
  - El controlador maneja el error y retorna 500

---

## 6. PROCEDIMIENTOS ADICIONALES (OPCIONALES)

### 6.1 sp_GetLogs - Consultar Logs

```sql
CREATE PROCEDURE [dbo].[sp_GetLogs]
    @StartDate DATETIME2(7) = NULL,
    @EndDate DATETIME2(7) = NULL,
    @Level INT = NULL,
    @Username NVARCHAR(255) = NULL,
    @Source NVARCHAR(255) = NULL,
    @MachineName NVARCHAR(255) = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 100
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Valores por defecto
    SET @PageNumber = ISNULL(@PageNumber, 1);
    SET @PageSize = ISNULL(@PageSize, 100);
    
    -- Limitar página máxima para evitar consultas costosas
    IF @PageSize > 1000
        SET @PageSize = 1000;
    
    SELECT 
        LogId, Id, Level, Message, Exception, StackTrace,
        Source, Method, Username, MachineName, AppVersion,
        Timestamp, CreatedAt, AdditionalData
    FROM ApplicationLogs
    WHERE 
        (@StartDate IS NULL OR Timestamp >= @StartDate)
        AND (@EndDate IS NULL OR Timestamp <= @EndDate)
        AND (@Level IS NULL OR Level = @Level)
        AND (@Username IS NULL OR Username = @Username)
        AND (@Source IS NULL OR Source = @Source)
        AND (@MachineName IS NULL OR MachineName = @MachineName)
    ORDER BY Timestamp DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
    
    -- Retornar también el total de registros
    SELECT COUNT(*) AS TotalRecords
    FROM ApplicationLogs
    WHERE 
        (@StartDate IS NULL OR Timestamp >= @StartDate)
        AND (@EndDate IS NULL OR Timestamp <= @EndDate)
        AND (@Level IS NULL OR Level = @Level)
        AND (@Username IS NULL OR Username = @Username)
        AND (@Source IS NULL OR Source = @Source)
        AND (@MachineName IS NULL OR MachineName = @MachineName);
END
GO
```

### 6.2 sp_CleanupOldLogs - Limpieza de Logs Antiguos

```sql
CREATE PROCEDURE [dbo].[sp_CleanupOldLogs]
    @DaysToKeep INT = 90,
    @BatchSize INT = 1000
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CutoffDate DATETIME2(7);
    DECLARE @RowsDeleted INT = 0;
    DECLARE @TotalDeleted INT = 0;
    
    -- Calcular fecha límite
    SET @CutoffDate = DATEADD(DAY, -@DaysToKeep, GETUTCDATE());
    
    -- Eliminar en lotes para evitar bloqueos largos
    WHILE 1 = 1
    BEGIN
        BEGIN TRANSACTION;
        
        DELETE TOP (@BatchSize)
        FROM ApplicationLogs
        WHERE Timestamp < @CutoffDate;
        
        SET @RowsDeleted = @@ROWCOUNT;
        SET @TotalDeleted = @TotalDeleted + @RowsDeleted;
        
        COMMIT TRANSACTION;
        
        -- Si eliminamos menos registros que el tamaño del lote, terminamos
        IF @RowsDeleted < @BatchSize
            BREAK;
            
        -- Pequeña pausa para evitar sobrecarga
        WAITFOR DELAY '00:00:01';
    END
    
    -- Retornar cantidad de registros eliminados
    SELECT @TotalDeleted AS TotalRecordsDeleted;
END
GO
```

### 6.3 sp_GetLogStatistics - Estadísticas de Logs

```sql
CREATE PROCEDURE [dbo].[sp_GetLogStatistics]
    @StartDate DATETIME2(7) = NULL,
    @EndDate DATETIME2(7) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Valores por defecto: últimos 30 días
    IF @StartDate IS NULL
        SET @StartDate = DATEADD(DAY, -30, GETUTCDATE());
        
    IF @EndDate IS NULL
        SET @EndDate = GETUTCDATE();
    
    -- Estadísticas por nivel de log
    SELECT 
        Level,
        CASE Level
            WHEN 0 THEN 'Trace'
            WHEN 1 THEN 'Debug'
            WHEN 2 THEN 'Information'
            WHEN 3 THEN 'Warning'
            WHEN 4 THEN 'Error'
            WHEN 5 THEN 'Critical'
        END AS LevelName,
        COUNT(*) AS Count,
        MIN(Timestamp) AS FirstOccurrence,
        MAX(Timestamp) AS LastOccurrence
    FROM ApplicationLogs
    WHERE Timestamp BETWEEN @StartDate AND @EndDate
    GROUP BY Level
    ORDER BY Level;
    
    -- Top 10 fuentes con más logs
    SELECT TOP 10
        Source,
        COUNT(*) AS Count
    FROM ApplicationLogs
    WHERE Timestamp BETWEEN @StartDate AND @EndDate
        AND Source IS NOT NULL
    GROUP BY Source
    ORDER BY Count DESC;
    
    -- Top 10 usuarios con más logs
    SELECT TOP 10
        Username,
        COUNT(*) AS Count
    FROM ApplicationLogs
    WHERE Timestamp BETWEEN @StartDate AND @EndDate
        AND Username IS NOT NULL
    GROUP BY Username
    ORDER BY Count DESC;
    
    -- Distribución por día
    SELECT 
        CAST(Timestamp AS DATE) AS LogDate,
        COUNT(*) AS Count
    FROM ApplicationLogs
    WHERE Timestamp BETWEEN @StartDate AND @EndDate
    GROUP BY CAST(Timestamp AS DATE)
    ORDER BY LogDate DESC;
END
GO
```

---

## 7. CONFIGURACIÓN DE LA APLICACIÓN

### 7.1 Actualización de appsettings.json

El archivo `appsettings.json` debe incluir la URL base del endpoint de logging:

```json
{
  "ExternalApi": {
    "BaseUrl": "https://localhost:7055/api/",
    "ApiKey": ""
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    },
    "RemoteLogging": {
      "Enabled": true,
      "MinimumLevel": "Warning"
    }
  }
}
```

### 7.2 Registro en Dependency Injection

El servicio se registra en `App.xaml.cs`:

```csharp
// Registrar LoggingService como HttpClient tipado
services.AddHttpClient<ILoggingService, LoggingService>((sp, client) =>
{
    var provider = sp.GetRequiredService<IApiEndpointProvider>();
    if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
    {
        client.BaseAddress = baseUri;
    }
    client.Timeout = TimeSpan.FromSeconds(5);
});
```

---

## 8. USO DEL SERVICIO DE LOGGING

### 8.1 Inyección del Servicio

```csharp
public class AuthService : IAuthService
{
    private readonly ILoggingService _logger;
    
    public AuthService(ILoggingService logger, /* otros parámetros */)
    {
        _logger = logger;
    }
}
```

### 8.2 Ejemplos de Uso

```csharp
// Log de información
await _logger.LogInformationAsync(
    "Usuario autenticado correctamente", 
    source: "AuthService", 
    method: "AuthenticateAsync"
);

// Log de error con excepción
catch (Exception ex)
{
    await _logger.LogErrorAsync(
        "Error al autenticar usuario", 
        exception: ex,
        source: "AuthService", 
        method: "AuthenticateAsync"
    );
}

// Log crítico
await _logger.LogCriticalAsync(
    "Fallo crítico en la conexión a la base de datos", 
    exception: ex,
    source: "DatabaseService", 
    method: "ConnectAsync"
);
```

---

## 9. CONSIDERACIONES DE SEGURIDAD

### 9.1 Autenticación del Endpoint
- El endpoint `/api/Logging/log` debería estar protegido con autenticación Bearer
- Solo clientes autenticados pueden enviar logs
- Validar el token en cada request

### 9.2 Validación de Datos
- Validar que el nivel de log sea válido (0-5)
- Sanitizar campos de texto para prevenir inyección SQL
- Limitar el tamaño máximo de los campos (StackTrace, Message, etc.)

### 9.3 Rate Limiting
- Implementar rate limiting para prevenir abuso
- Ejemplo: Máximo 100 logs por minuto por cliente
- Bloquear temporalmente clientes que excedan el límite

### 9.4 Datos Sensibles
- No registrar información sensible en logs (contraseñas, tokens, etc.)
- Implementar ofuscación automática de datos sensibles
- Cumplir con regulaciones de privacidad (GDPR, etc.)

---

## 10. MONITOREO Y MANTENIMIENTO

### 10.1 Tareas de Mantenimiento Recomendadas

1. **Limpieza Automática**
   - Ejecutar `sp_CleanupOldLogs` semanalmente
   - Mantener logs por 90 días (configurable)
   - Archivar logs críticos antes de eliminar

2. **Monitoreo de Crecimiento**
   - Monitorear el tamaño de la tabla `ApplicationLogs`
   - Alertar si crece más de X GB por día
   - Considerar particionamiento si el volumen es muy alto

3. **Análisis de Logs**
   - Ejecutar `sp_GetLogStatistics` diariamente
   - Identificar patrones de errores recurrentes
   - Alertar sobre incrementos en logs críticos

### 10.2 Job de SQL Server Agent

```sql
-- Job para limpieza automática de logs antiguos
-- Ejecutar cada domingo a las 2:00 AM
USE [msdb]
GO

EXEC msdb.dbo.sp_add_job
    @job_name = N'CleanupApplicationLogs',
    @enabled = 1,
    @description = N'Elimina logs de aplicación con más de 90 días'
GO

EXEC msdb.dbo.sp_add_jobstep
    @job_name = N'CleanupApplicationLogs',
    @step_name = N'Execute Cleanup',
    @subsystem = N'TSQL',
    @command = N'EXEC sp_CleanupOldLogs @DaysToKeep = 90, @BatchSize = 1000;',
    @database_name = N'AdvanceControlDB'
GO

EXEC msdb.dbo.sp_add_schedule
    @schedule_name = N'Weekly Sunday 2AM',
    @freq_type = 8, -- Weekly
    @freq_interval = 1, -- Sunday
    @active_start_time = 20000 -- 2:00 AM
GO

EXEC msdb.dbo.sp_attach_schedule
    @job_name = N'CleanupApplicationLogs',
    @schedule_name = N'Weekly Sunday 2AM'
GO
```

---

## 11. PRÓXIMOS PASOS

### 11.1 Implementación Inmediata

- [x] Crear servicio de logging en cliente (ILoggingService, LoggingService)
- [x] Crear modelo de datos (LogEntry)
- [ ] Actualizar App.xaml.cs para registrar el servicio
- [ ] Añadir logging a todos los bloques try-catch existentes

### 11.2 Implementación en el Servidor (Pendiente)

1. **Controlador API**
   - Crear `LoggingController` en el proyecto API
   - Implementar endpoint POST `/api/Logging/log`
   - Configurar autenticación y validación

2. **Base de Datos**
   - Crear tabla `ApplicationLogs`
   - Crear índices necesarios
   - Crear procedimiento almacenado `sp_InsertLog`

3. **Procedimientos Adicionales**
   - `sp_GetLogs` - Consultar logs con filtros
   - `sp_CleanupOldLogs` - Limpieza automática
   - `sp_GetLogStatistics` - Estadísticas y análisis

4. **Configuración**
   - Configurar connection string en appsettings del API
   - Configurar rate limiting
   - Configurar jobs de mantenimiento

### 11.3 Mejoras Futuras

- Dashboard de visualización de logs
- Alertas en tiempo real para logs críticos
- Integración con servicios de monitoring (Application Insights, etc.)
- Exportación de logs a formatos estándar (CSV, JSON)
- Búsqueda full-text en mensajes de log

---

## 12. CONTACTO Y SOPORTE

Para preguntas o problemas relacionados con el sistema de logging:
- Revisar este documento primero
- Verificar logs del servidor API
- Verificar conectividad del cliente

---

**Fin del Reporte**
