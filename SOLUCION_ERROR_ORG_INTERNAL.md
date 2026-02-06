# Solución de Problemas: Autenticación Google Cloud Storage

## Error 403: org_internal

### Síntomas
Al intentar autenticarse con Google Cloud Storage, aparece un error en el navegador similar a:

```
Error 403: org_internal

Request details:
access_type=offline
scope=https://www.googleapis.com/auth/devstorage.read_write
response_type=code
redirect_uri=http://127.0.0.1:8484/
...
client_id=696897558393-v866s22cfqialum2u033sia0rt3ra6lm.apps.googleusercontent.com
```

### Causa
Este error ocurre cuando el cliente OAuth 2.0 ("Advance Control") está configurado como **"Interno"** en Google Cloud Console. Esto significa que solo los usuarios dentro de la organización de Google Workspace pueden autenticarse.

### Solución

Para permitir que cualquier usuario con cuenta de Google pueda autenticarse:

1. **Acceder a Google Cloud Console**
   - Ir a [Google Cloud Console](https://console.cloud.google.com/)
   - Seleccionar el proyecto que contiene el cliente OAuth "Advance Control"

2. **Navegar a la Pantalla de Consentimiento OAuth**
   - En el menú lateral, ir a **APIs y servicios** → **Pantalla de consentimiento OAuth**

3. **Cambiar el tipo de usuario**
   - En la sección "Tipo de usuario", hacer clic en **"Hacer externo"** o cambiar de "Interno" a "Externo"
   - Nota: Esta opción puede requerir permisos de administrador del proyecto

4. **Configurar la pantalla de consentimiento** (si es necesario)
   - Completar la información requerida:
     - Nombre de la aplicación: `Advance Control`
     - Correo electrónico de soporte
     - Logotipo (opcional)
     - Dominio de la aplicación (opcional para desarrollo)
     - Correos autorizados para pruebas

5. **Publicar la aplicación** (para producción)
   - Si la aplicación necesita acceso público, enviar a verificación de Google
   - Para desarrollo/pruebas, se puede usar en modo "Testing" agregando usuarios de prueba

### Estado de Pruebas vs Producción

| Estado | Descripción | Límites |
|--------|-------------|---------|
| **Testing** | Solo usuarios agregados manualmente pueden autenticarse | Máximo 100 usuarios de prueba |
| **Producción** | Cualquier usuario de Google puede autenticarse | Requiere verificación de Google si solicita scopes sensibles |

### Agregar Usuarios de Prueba (Modo Testing)

1. En la pantalla de consentimiento OAuth, ir a la sección **"Usuarios de prueba"**
2. Hacer clic en **"+ Agregar usuarios"**
3. Ingresar los correos electrónicos de los usuarios que necesitan acceder
4. Guardar los cambios

### Verificación de la Configuración

Para verificar que la configuración es correcta:

1. El tipo de usuario debe ser **"Externo"**
2. El estado de publicación puede ser "Testing" o "En producción"
3. Los scopes deben incluir:
   - `https://www.googleapis.com/auth/devstorage.read_write`

### Otros Errores Comunes

| Error | Causa | Solución |
|-------|-------|----------|
| `access_denied` | Usuario denegó permisos | Pedir al usuario que autorice la aplicación |
| `invalid_client` | ClientId o ClientSecret incorrectos | Verificar credenciales en `appsettings.json` |
| `invalid_grant` | Código de autorización expirado | Intentar la autenticación nuevamente |
| `redirect_uri_mismatch` | URI de redirección no coincide | Verificar que `http://127.0.0.1:8484/` está configurado en Google Cloud Console |

### Configuración en appsettings.json

```json
{
  "GoogleCloudStorage": {
    "ClientId": "tu-client-id.apps.googleusercontent.com",
    "ClientSecret": "tu-client-secret",
    "BucketName": "nombre-de-tu-bucket",
    "RedirectUri": "http://127.0.0.1:8484/"
  }
}
```

### Verificar Credenciales OAuth

1. En Google Cloud Console, ir a **APIs y servicios** → **Credenciales**
2. Buscar el cliente OAuth 2.0 llamado "Advance Control"
3. Hacer clic para editar y verificar:
   - El tipo es "Aplicación de escritorio" o "Aplicación web"
   - Los URIs de redirección autorizados incluyen `http://127.0.0.1:8484/`
   - Los orígenes de JavaScript autorizados (si aplica)

### Contacto de Soporte

Si después de seguir estos pasos el problema persiste, contactar al administrador del proyecto de Google Cloud para verificar los permisos y la configuración de la organización.
