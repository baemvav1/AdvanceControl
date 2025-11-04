using System;
using System.Collections.Generic;
using System.Linq;
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
            catch (Exception ex)
            {
                _logger?.LogDebugAsync($"Credencial no existe previamente al intentar actualizar: {key}", "SecretStorageWindows", "SetAsync");
                // Retrieve lanza si no existe; ignorar
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
            catch (Exception ex)
            {
                _logger?.LogDebugAsync($"No se encontró credencial en almacenamiento seguro: {key}", "SecretStorageWindows", "GetAsync");
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
            catch (Exception ex)
            {
                _logger?.LogDebugAsync($"Error al eliminar credencial (posiblemente no existe): {key}", "SecretStorageWindows", "RemoveAsync");
                // Si no existe, ignorar
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
                _logger?.LogErrorAsync("Error al limpiar almacenamiento seguro", ex, "SecretStorageWindows", "ClearAsync");
                // ignorar errores
            }

            return Task.CompletedTask;
        }
    }
}
