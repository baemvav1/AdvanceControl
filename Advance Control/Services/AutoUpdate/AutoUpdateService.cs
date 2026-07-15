using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace Advance_Control.Services.AutoUpdate
{
    public class AutoUpdateService : IAutoUpdateService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IApiEndpointProvider _endpoints;

        public AutoUpdateService(IHttpClientFactory httpFactory, IApiEndpointProvider endpoints)
        {
            _httpFactory = httpFactory;
            _endpoints = endpoints;
        }

        public async Task CheckAndPromptAsync(CancellationToken ct = default)
        {
            try
            {
                var remoteVersion = await GetRemoteVersionAsync(ct);
                if (remoteVersion == null) return;

                var currentVersion = GetCurrentPackageVersion();
                if (currentVersion == null) return;

                if (remoteVersion <= currentVersion) return;

                await ShowUpdateDialogAsync(remoteVersion, ct);
            }
            catch
            {
                // Never interrupt the user for update failures
            }
        }

        private async Task<Version?> GetRemoteVersionAsync(CancellationToken ct)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(5));

                var http = _httpFactory.CreateClient("AutoUpdate");
                var dto = await http.GetFromJsonAsync<ClienteVersionDto>(
                    "client-dist/version.json",
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                    cts.Token);

                if (dto == null || string.IsNullOrWhiteSpace(dto.Version)) return null;
                return Version.TryParse(dto.Version, out var v) ? v : null;
            }
            catch
            {
                return null;
            }
        }

        private static Version? GetCurrentPackageVersion()
        {
            try
            {
                var v = Package.Current.Id.Version;
                return new Version(v.Major, v.Minor, v.Build, v.Revision);
            }
            catch
            {
                // Running unpackaged (dev mode) — skip update check
                return null;
            }
        }

        private async Task ShowUpdateDialogAsync(Version remoteVersion, CancellationToken ct)
        {
            var xamlRoot = App.MainWindow?.Content?.XamlRoot;
            if (xamlRoot == null) return;

            var confirmDialog = new ContentDialog
            {
                Title = "Actualización disponible",
                Content = $"Hay una nueva versión de Advance Control (v{remoteVersion}).\n\nLa aplicación se cerrará brevemente para instalar la actualización.",
                PrimaryButtonText = "Instalar ahora",
                CloseButtonText = "Después",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = xamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            await DownloadAndInstallAsync(remoteVersion, xamlRoot, ct);
        }

        private async Task DownloadAndInstallAsync(Version remoteVersion, XamlRoot xamlRoot, CancellationToken ct)
        {
            var progressRing = new ProgressRing
            {
                IsActive = true,
                Width = 48,
                Height = 48,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var progressDialog = new ContentDialog
            {
                Title = "Descargando actualización...",
                Content = progressRing,
                XamlRoot = xamlRoot
            };

            // Show progress dialog without awaiting — we'll hide it when done
            _ = progressDialog.ShowAsync();

            try
            {
                var msixUrl = "client-dist/AdvanceControl-x64.msix";
                var tempPath = Path.Combine(Path.GetTempPath(), $"AdvanceControl-{remoteVersion}.msix");

                var http = _httpFactory.CreateClient("AutoUpdate");
                using var response = await http.GetAsync(msixUrl, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();

                await using var networkStream = await response.Content.ReadAsStreamAsync(ct);
                await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
                await networkStream.CopyToAsync(fileStream, ct);

                progressDialog.Hide();

                Process.Start(new ProcessStartInfo { FileName = tempPath, UseShellExecute = true });
                Application.Current.Exit();
            }
            catch
            {
                progressDialog.Hide();

                var errorDialog = new ContentDialog
                {
                    Title = "Error al descargar",
                    Content = "No se pudo descargar la actualización. Inténtalo más tarde.",
                    CloseButtonText = "Cerrar",
                    XamlRoot = xamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }
    }
}
