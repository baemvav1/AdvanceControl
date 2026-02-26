using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Advance_Control.Services.NotificationSettings
{
    /// <summary>
    /// Implementación singleton de INotificationSettingsService.
    /// Persiste la lista en ApplicationData.Current.LocalFolder\notification_settings.json.
    /// Thread-safe para escrituras concurrentes.
    /// </summary>
    public class NotificationSettingsService : INotificationSettingsService
    {
        private const string FileName = "notification_settings.json";

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, NotificacionPermitidaEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
        private bool _loaded;

        // ── Carga lazy sincronizada ───────────────────────────────────────────

        private async Task EnsureLoadedAsync()
        {
            if (_loaded) return;

            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_loaded) return;
                await LoadFromFileAsync().ConfigureAwait(false);
                _loaded = true;
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task LoadFromFileAsync()
        {
            try
            {
                var folder = GetLocalFolder();
                var filePath = Path.Combine(folder, FileName);

                if (!File.Exists(filePath)) return;

                var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
                var root = JsonSerializer.Deserialize<SettingsRoot>(json, _jsonOptions);

                if (root?.NotificacionesPermitidas is not null)
                {
                    foreach (var entry in root.NotificacionesPermitidas)
                    {
                        if (!string.IsNullOrWhiteSpace(entry.Categoria))
                            _entries[entry.Categoria] = entry;
                    }
                }
            }
            catch
            {
                // Archivo corrupto o inexistente → empezar con lista vacía
            }
        }

        // ── Interfaz pública ─────────────────────────────────────────────────

        public async Task SetCategoryEnabledAsync(string categoria, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(categoria)) return;

            await EnsureLoadedAsync().ConfigureAwait(false);

            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_entries.TryGetValue(categoria, out var entry))
                {
                    entry.Habilitada = enabled;
                }
                else
                {
                    _entries[categoria] = new NotificacionPermitidaEntry
                    {
                        Categoria = categoria,
                        Habilitada = enabled
                    };
                }
                await SaveToFileAsync().ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task EnsureCategoryRegisteredAsync(string? categoria, string? page = null)
        {
            if (string.IsNullOrWhiteSpace(categoria)) return;

            await EnsureLoadedAsync().ConfigureAwait(false);

            if (_entries.ContainsKey(categoria)) return;

            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                // Double-check dentro del lock
                if (_entries.ContainsKey(categoria)) return;

                _entries[categoria] = new NotificacionPermitidaEntry
                {
                    Categoria = categoria,
                    Page = page,
                    Habilitada = true
                };

                await SaveToFileAsync().ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }
        }

        public bool IsCategoryAllowed(string? categoria)
        {
            // Sin categoría → siempre visible
            if (string.IsNullOrWhiteSpace(categoria)) return true;

            // Si aún no se cargó (llamado sincrónico muy temprano), no filtrar
            if (!_loaded) return true;

            return _entries.TryGetValue(categoria, out var entry) && entry.Habilitada;
        }

        public IReadOnlyList<NotificacionPermitidaEntry> GetAll()
        {
            return _entries.Values.ToList().AsReadOnly();
        }

        // ── Persistencia ─────────────────────────────────────────────────────

        private async Task SaveToFileAsync()
        {
            try
            {
                var folder = GetLocalFolder();
                Directory.CreateDirectory(folder);

                var root = new SettingsRoot
                {
                    NotificacionesPermitidas = _entries.Values.ToList()
                };

                var json = JsonSerializer.Serialize(root, _jsonOptions);
                await File.WriteAllTextAsync(Path.Combine(folder, FileName), json).ConfigureAwait(false);
            }
            catch
            {
                // Silenciar: el sistema de notificaciones nunca debe interrumpir el flujo
            }
        }

        private static string GetLocalFolder()
        {
            try
            {
                // En apps empaquetadas (MSIX), ApplicationData.Current.LocalFolder es la ruta correcta
                return ApplicationData.Current.LocalFolder.Path;
            }
            catch
            {
                // Fallback para modo desarrollo sin paquete
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AdvanceControl");
            }
        }

        // ── Tipos internos para serialización ────────────────────────────────

        private sealed class SettingsRoot
        {
            public List<NotificacionPermitidaEntry> NotificacionesPermitidas { get; set; } = new();
        }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }
}
