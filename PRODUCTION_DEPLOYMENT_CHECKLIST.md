# Lista de Verificación para Despliegue en Producción

**Proyecto:** Advance Control  
**Versión:** 1.0  
**Última Actualización:** 2025-11-16

---

## ⚠️ IMPORTANTE

Este documento debe completarse **COMPLETAMENTE** antes de desplegar la aplicación en un entorno de producción. Cada elemento marcado como **CRÍTICO** puede resultar en vulnerabilidades de seguridad si no se atiende.

---

## 🔒 Seguridad y Configuración

### Crítico ❌

- [ ] **DevelopmentMode.Enabled = false** en `appsettings.json`
  - Ubicación: `Advance Control/appsettings.json`
  - Verificar que: `"Enabled": false`
  - Riesgo si no se hace: Bypasses de seguridad activos en producción

- [ ] **BaseUrl** apunta al servidor de producción real
  - Ubicación: `Advance Control/appsettings.json`
  - Cambiar de: `https://localhost:7055/`
  - Cambiar a: `https://api.su-dominio.com/` (URL real)
  - Riesgo si no se hace: Aplicación no funcional

- [ ] **BaseUrl usa HTTPS** (no HTTP)
  - Verificar que comience con: `https://`
  - Riesgo si no se hace: Tokens y datos sensibles expuestos en tránsito

- [ ] **Certificado SSL válido y no expirado**
  - Verificar en servidor: Certificado válido
  - Verificar fecha de expiración: Debe ser futura
  - Riesgo si no se hace: Warnings de seguridad, usuarios no confían

- [ ] **Eliminar/cambiar credenciales de prueba**
  - Verificar: No hay usuarios con passwords por defecto
  - Verificar: Cuentas de prueba eliminadas o deshabilitadas
  - Riesgo si no se hace: Acceso no autorizado

### Importante ⚠️

- [ ] **DisableAuthTimeouts = false**
  - Ubicación: `Advance Control/appsettings.json`
  - Verificar que: `"DisableAuthTimeouts": false`
  - Riesgo si no se hace: Tokens nunca expiran

- [ ] **DisableHttpTimeouts = false**
  - Ubicación: `Advance Control/appsettings.json`
  - Verificar que: `"DisableHttpTimeouts": false`
  - Riesgo si no se hace: Requests infinitos consumiendo recursos

- [ ] **Logging configurado apropiadamente**
  - Nivel recomendado: `"Warning"` o `"Error"`
  - No usar: `"Debug"` o `"Trace"` en producción
  - Riesgo si no se hace: Logs excesivos, información sensible en logs

- [ ] **ApiKey eliminada o validada**
  - Si no se usa: Eliminar la propiedad `"ApiKey": ""`
  - Si se usa: Asegurar que no esté vacía y sea secreta
  - Riesgo si no se hace: Confusión o exposición de API keys

### Recomendado ✅

- [ ] Crear `appsettings.Production.json` separado
- [ ] Usar variables de entorno para secretos
- [ ] Configurar Content Security Policy (si aplica)
- [ ] Implementar rate limiting en el servidor
- [ ] Habilitar auditoría de accesos

---

## 🧪 Testing y Calidad

### Crítico ❌

- [ ] **Tests unitarios pasando**
  - Comando: `dotnet test`
  - Resultado esperado: 100% pass
  - Riesgo si no se hace: Bugs conocidos en producción

- [ ] **Tests de integración pasando**
  - Verificar: Comunicación con API real funciona
  - Verificar: Autenticación end-to-end funciona
  - Riesgo si no se hace: Funcionalidad crítica rota

### Importante ⚠️

- [ ] **Tests de seguridad realizados**
  - Penetration testing básico completado
  - Vulnerabilidades de OWASP Top 10 verificadas
  - SQL/NoSQL injection tests
  - XSS tests (si aplica)

- [ ] **Performance testing**
  - Carga esperada soportada
  - Timeouts apropiados configurados
  - Memory leaks descartados

- [ ] **Compatibilidad verificada**
  - Windows 10 versión mínima: 17763
  - Windows 11 compatible
  - Diferentes resoluciones de pantalla

---

## 🗄️ Base de Datos y Backend

### Crítico ❌

- [ ] **Backup de base de datos configurado**
  - Frecuencia: Diaria como mínimo
  - Retención: Al menos 30 días
  - Tested: Proceso de restore verificado

- [ ] **API servidor accesible**
  - Endpoint `/Online` responde 200 OK
  - Endpoint `/api/Auth/login` funcional
  - Endpoint `/api/Clientes` funcional

- [ ] **Credenciales de base de datos seguras**
  - No usar credenciales por defecto
  - Usar least privilege principle
  - Rotar passwords periódicamente

### Importante ⚠️

- [ ] Migrations de base de datos aplicadas
- [ ] Índices de base de datos optimizados
- [ ] Monitoreo de performance de queries
- [ ] Plan de escalabilidad definido

---

## 📦 Despliegue y Distribución

### Crítico ❌

- [ ] **Build de Release configurado correctamente**
  - No usar Debug build
  - PublishTrimmed = True (para producción)
  - PublishReadyToRun = True (para producción)

- [ ] **Certificado de firma de código**
  - Aplicación firmada con certificado válido
  - Certificado no expirado
  - Riesgo si no se hace: Windows SmartScreen warnings

- [ ] **Versión incrementada**
  - Version en .csproj actualizada
  - Assembly version incrementada
  - Changelog actualizado

### Importante ⚠️

- [ ] **Package MSIX creado**
  - Aplicación empaquetada correctamente
  - Assets incluidos (iconos, splash screen)
  - Manifest configurado correctamente

- [ ] **Instalador probado**
  - Instalación limpia exitosa
  - Actualización desde versión anterior exitosa
  - Desinstalación limpia (no deja archivos)

- [ ] **Documentación de usuario actualizada**
  - Manual de usuario actualizado
  - FAQ actualizado
  - Troubleshooting guide actualizado

---

## 🔐 Seguridad Post-Despliegue

### Crítico ❌

- [ ] **Monitoreo de logs de seguridad**
  - Alertas configuradas para intentos de login fallidos
  - Alertas configuradas para errores 401/403
  - Dashboard de monitoreo accesible

- [ ] **Plan de respuesta a incidentes**
  - Contactos de emergencia definidos
  - Proceso de escalamiento documentado
  - Rollback plan preparado

### Importante ⚠️

- [ ] **Actualizaciones de seguridad**
  - Proceso de actualización definido
  - Calendario de parches establecido
  - Notificaciones a usuarios configuradas

- [ ] **Auditoría de accesos**
  - Logs de autenticación habilitados
  - Revisión periódica de accesos
  - Detección de anomalías configurada

---

## 📊 Monitoreo y Observabilidad

### Importante ⚠️

- [ ] **Application Insights / Telemetría**
  - Telemetría básica habilitada
  - Métricas de performance monitoreadas
  - Error tracking configurado

- [ ] **Health checks**
  - Endpoint de health check implementado
  - Monitoreo automático configurado
  - Alertas en caso de degradación

- [ ] **Métricas de negocio**
  - Usuarios activos monitoreados
  - Operaciones críticas trackeadas
  - SLA metrics definidos

---

## 🚀 Lanzamiento

### Día del Lanzamiento

- [ ] **Ventana de mantenimiento comunicada**
  - Usuarios notificados con 48h de anticipación
  - Tiempo estimado de downtime comunicado
  - Canal de soporte preparado

- [ ] **Backup pre-despliegue**
  - Backup completo de base de datos
  - Backup de configuración actual
  - Versión anterior accesible para rollback

- [ ] **Despliegue en horario valle**
  - Preferiblemente fuera de horario laboral
  - Equipo completo disponible
  - Plan de rollback listo

### Post-Lanzamiento (Primeras 24h)

- [ ] **Monitoreo intensivo**
  - Verificar logs cada 2 horas
  - Verificar métricas de performance
  - Verificar reportes de usuarios

- [ ] **Smoke tests en producción**
  - Login de usuario exitoso
  - Operaciones críticas funcionan
  - No hay errores en logs

- [ ] **Comunicación con usuarios**
  - Confirmar que lanzamiento fue exitoso
  - Recopilar feedback inicial
  - Resolver issues urgentes

---

## 📋 Checklist de Verificación Técnica Detallada

### appsettings.json

```json
{
  "ExternalApi": {
    "BaseUrl": "https://api.produccion.com/",  // ✅ HTTPS, no localhost
    "ApiKey": ""                                 // ✅ Eliminado si no se usa
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning"                       // ✅ Warning o Error
    }
  },
  "DevelopmentMode": {
    "Enabled": false,                            // ✅ CRÍTICO: false
    "DisableAuthTimeouts": false,                // ✅ false
    "DisableHttpTimeouts": false                 // ✅ false
  }
}
```

### Advance Control.csproj

```xml
<PropertyGroup>
  <Version>1.0.0</Version>                       <!-- ✅ Actualizado -->
  <PublishTrimmed>True</PublishTrimmed>          <!-- ✅ True para Release -->
  <PublishReadyToRun>True</PublishReadyToRun>    <!-- ✅ True para Release -->
</PropertyGroup>
```

---

## 🔄 Plan de Rollback

En caso de problemas críticos:

1. **Detener despliegue inmediatamente**
2. **Restaurar backup de base de datos**
3. **Desplegar versión anterior de la aplicación**
4. **Comunicar a usuarios sobre el rollback**
5. **Investigar causa raíz del problema**
6. **Planificar nuevo despliegue con fix**

---

## ✅ Firmas de Aprobación

Antes del despliegue, los siguientes roles deben aprobar:

- [ ] **Tech Lead / Arquitecto**
  - Nombre: _________________
  - Fecha: _________________

- [ ] **Security Officer**
  - Nombre: _________________
  - Fecha: _________________

- [ ] **QA Lead**
  - Nombre: _________________
  - Fecha: _________________

- [ ] **Product Owner**
  - Nombre: _________________
  - Fecha: _________________

---

## 📞 Contactos de Emergencia

| Rol | Nombre | Teléfono | Email |
|-----|--------|----------|-------|
| Tech Lead | ________ | ________ | ________ |
| DevOps | ________ | ________ | ________ |
| Security | ________ | ________ | ________ |
| Product Owner | ________ | ________ | ________ |

---

## 📚 Referencias

- `SECURITY_REVIEW_REPORT.md` - Reporte completo de seguridad
- `SECURITY_IMPROVEMENTS_SUMMARY.md` - Resumen de mejoras aplicadas
- `appsettings.Production.json` - Template de configuración para producción

---

**NOTA FINAL:** Este checklist debe revisarse y actualizarse periódicamente. Cada despliegue debe completar este documento como parte del proceso estándar.

---

**Versión del Checklist:** 1.0  
**Última Actualización:** 2025-11-16  
**Próxima Revisión:** [Fecha futura]
