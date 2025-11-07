using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.Security
{
    public class SecretStorageWindows : ISecureStorage
    {
        private readonly PasswordVault _vault = new();
        private readonly ILoggingService? _logger;

        // Prefijo para distinguir entradas de esta app
        private const string ResourcePrefix = "Advance_Control";

        public SecretStorageWindows(ILoggingService? logger = null)
        {
            _logger = logger;
        }

        private static string ResourceForKey(string key) =>
            $"{ResourcePrefix}:{key}";

        public Task SetAsync(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException(nameof(key));
            if (value is null) throw new ArgumentNullException(nameof(value));

            var resource = ResourceForKey(key);

            // Si existe, primero eliminar la entrada previa para evitar duplicados
            try
            {
                var existing = _vault.Retrieve(resource, key);
                _vault.Remove(existing);
            }
            catch (COMException ex) when (ex.HResult == unchecked((int)0x80070490))
            {
                // HRESULT 0x80070490 = ERROR_NOT_FOUND (Element not found)
                // Esto es esperado cuando la credencial no existe previamente
                _ = _logger?.LogDebugAsync($"Credencial no existe previamente (normal para nuevos usuarios): {key}", "SecretStorageWindows", "SetAsync");
            }
            catch (Exception ex)
            {
                _ = _logger?.LogWarningAsync($"Error al verificar credencial existente: {key}. Error: {ex.Message}", "SecretStorageWindows", "SetAsync");
                // Continuar de todos modos e intentar agregar
            }

            // Añadir la nueva credencial
            var cred = new PasswordCredential(resource, key, value);
            _vault.Add(cred);

            return Task.CompletedTask;
        }

        public Task<string?> GetAsync(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException(nameof(key));
            var resource = ResourceForKey(key);

            try
            {
                var cred = _vault.Retrieve(resource, key);
                // Must call Retrieve then Password to get the secret
                cred.RetrievePassword();
                return Task.FromResult<string?>(cred.Password);
            }
            catch (COMException ex) when (ex.HResult == unchecked((int)0x80070490))
            {
                // HRESULT 0x80070490 = ERROR_NOT_FOUND (Element not found)
                // This is expected when the credential doesn't exist (e.g., first-time user)
                _ = _logger?.LogDebugAsync($"Credencial no encontrada en almacenamiento seguro: {key}. Esto es normal para usuarios nuevos.", "SecretStorageWindows", "GetAsync");
                return Task.FromResult<string?>(null);
            }
            catch (Exception ex)
            {
                _ = _logger?.LogWarningAsync($"Error inesperado al recuperar credencial del almacenamiento seguro: {key}. Error: {ex.Message}", "SecretStorageWindows", "GetAsync");
                return Task.FromResult<string?>(null);
            }
        }

        public Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException(nameof(key));
            var resource = ResourceForKey(key);

            try
            {
                var cred = _vault.Retrieve(resource, key);
                _vault.Remove(cred);
            }
            catch (COMException ex) when (ex.HResult == unchecked((int)0x80070490))
            {
                // HRESULT 0x80070490 = ERROR_NOT_FOUND (Element not found)
                // Esto es esperado cuando la credencial no existe
                _ = _logger?.LogDebugAsync($"Credencial no encontrada al intentar eliminar: {key}. Esto es normal si ya fue eliminada o nunca existió.", "SecretStorageWindows", "RemoveAsync");
            }
            catch (Exception ex)
            {
                _ = _logger?.LogWarningAsync($"Error inesperado al eliminar credencial: {key}. Error: {ex.Message}", "SecretStorageWindows", "RemoveAsync");
            }

            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            // Remove all entries that start with ResourcePrefix
            try
            {
                var all = _vault.RetrieveAll();
                var toRemove = all.Where(c => c.Resource != null && c.Resource.StartsWith(ResourcePrefix + ":")).ToList();
                foreach (var c in toRemove)
                {
                    // Need to call Retrieve to be able to Remove in some runtimes
                    try { _vault.Retrieve(c.Resource, c.UserName); } catch { }
                    _vault.Remove(c);
                }
            }
            catch (Exception ex)
            {
                _ = _logger?.LogErrorAsync("Error al limpiar almacenamiento seguro", ex, "SecretStorageWindows", "ClearAsync");
                // ignorar errores
            }

            return Task.CompletedTask;
        }
    }
}
