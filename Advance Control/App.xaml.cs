using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using Advance_Control.Services.OnlineCheck;
using Advance_Control.Services.EndPointProvider;
using Microsoft.Extensions.Configuration;

namespace Advance_Control
{
    public partial class App : Application
    {
        public IHost Host { get; }

        public App()
        {
            this.InitializeComponent();

            Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                // Asegúrate de que CreateDefaultBuilder cargue appsettings.json desde el output.
                // En WinUI, el appsettings.json debe estar marcado como "Copy to Output Directory" = "Copy if newer".
                .ConfigureAppConfiguration(cfg =>
                {
                    // Agregar appsettings.json por si no se cargó automáticamente
                    cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    // Enlazar sección "ExternalApi" de appsettings.json a ExternalApiOptions
                    services.Configure<ExternalApiOptions>(context.Configuration.GetSection("ExternalApi"));

                    // Registrar el provider que compone endpoints (usa IOptions<ExternalApiOptions>)
                    services.AddSingleton<IApiEndpointProvider, ApiEndpointProvider>();

                    // Registrar OnlineCheck como typed HttpClient (seguirá usando provider para construir endpoints)
                    services.AddHttpClient<IOnlineCheck, OnlineCheck>(client =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(5);
                    });

                    services.AddSingleton<Advance_Control.Services.Security.ISecureStorage, Advance_Control.Services.Security.SecretStorageWindows>();

                    // Registrar MainWindow para que DI pueda resolverlo y proporcionar sus dependencias
                    services.AddTransient<MainWindow>();
                })
                .Build();
        }

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // Iniciar el host (permite que HostedServices si existen arranquen)
            await Host.StartAsync();

            // Resolver MainWindow vía DI (NO crear con new)
            var window = Host.Services.GetRequiredService<MainWindow>();

            // Suscribirse al evento Closed de la ventana principal para detener y disponer el Host
            // Esto evita intentar sobrescribir OnExit (no disponible en WinUI 3).
            window.Closed += async (s, e) =>
            {
                try
                {
                    // Intentar detener el host de forma ordenada (timeout 5s)
                    await Host.StopAsync(TimeSpan.FromSeconds(5));
                }
                catch
                {
                    // Ignorar errores durante el cierre para no bloquear la salida de la app
                }
                finally
                {
                    Host.Dispose();
                }
            };

            window.Activate();
        }
    }
}