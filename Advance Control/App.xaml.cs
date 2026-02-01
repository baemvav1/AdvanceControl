using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System;
using Advance_Control.Services.OnlineCheck;
using Advance_Control.Services.EndPointProvider;
using Microsoft.Extensions.Configuration;
using Advance_Control.Services.Auth;
using Advance_Control.Services.Security;
using Advance_Control.Services.Http;
using Advance_Control.Services.Logging;
using Advance_Control.Navigation;
using Advance_Control.Services.Dialog;
using Advance_Control.Services.Clientes;
using Advance_Control.Services.Equipos;
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.UserInfo;
using Advance_Control.Services.Relaciones;
using Advance_Control.Services.Mantenimiento;
using Advance_Control.Services.Refacciones;
using Advance_Control.Services.RelacionesRefaccionEquipo;
using Advance_Control.Services.Proveedores;
using Advance_Control.Services.Operaciones;
using Advance_Control.Services.Cargos;
using Advance_Control.Services.Servicios;
using Advance_Control.Services.Quotes;
using Advance_Control.Services.GoogleMaps;
using Advance_Control.Services.Areas;

namespace Advance_Control
{
    public partial class App : Application
    {
        public IHost Host { get; }
        
        // Almacena la referencia a la ventana principal para acceder a XamlRoot
        public static Window? MainWindow { get; private set; }

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

                    // Enlazar sección "DevelopmentMode" de appsettings.json a DevelopmentModeOptions
                    services.Configure<Settings.DevelopmentModeOptions>(context.Configuration.GetSection("DevelopmentMode"));

                    // Registrar el provider que compone endpoints (usa IOptions<ExternalApiOptions>)
                    services.AddSingleton<IApiEndpointProvider, ApiEndpointProvider>();

                    // Registrar OnlineCheck como typed HttpClient (seguirá usando provider para construir endpoints)
                    services.AddHttpClient<IOnlineCheck, OnlineCheck>((sp, client) =>
                    {
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        if (devMode?.Enabled == true && devMode.DisableHttpTimeouts)
                        {
                            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                        }
                        else
                        {
                            client.Timeout = TimeSpan.FromSeconds(5);
                        }
                    });

                    // Registrar implementación de almacenamiento seguro (Windows PasswordVault)
                    services.AddSingleton<ISecureStorage, SecretStorageWindows>();

                    // Registrar AuthenticatedHttpHandler con Lazy<IAuthService> para romper dependencia circular
                    services.AddTransient<Services.Http.AuthenticatedHttpHandler>(sp =>
                    {
                        var lazyAuthService = new Lazy<IAuthService>(() => sp.GetRequiredService<IAuthService>());
                        var endpointProvider = sp.GetRequiredService<IApiEndpointProvider>();
                        var logger = sp.GetService<ILoggingService>(); // optional
                        return new Services.Http.AuthenticatedHttpHandler(lazyAuthService, endpointProvider, logger);
                    });

                    // Registrar LoggingService como HttpClient tipado
                    services.AddHttpClient<ILoggingService, LoggingService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                        {
                            client.BaseAddress = baseUri;
                        }
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        if (devMode?.Enabled == true && devMode.DisableHttpTimeouts)
                        {
                            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                        }
                        else
                        {
                            client.Timeout = TimeSpan.FromSeconds(5);
                        }
                    });

                    // Registrar AuthService y su HttpClient pipeline.
                    // Configuramos BaseAddress usando la IApiEndpointProvider registrada.
                    // NOTA: No se agrega AuthenticatedHttpHandler aquí para evitar dependencia circular
                    // (AuthService maneja endpoints de autenticación que no requieren Bearer token)
                    services.AddHttpClient<IAuthService, AuthService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                        {
                            client.BaseAddress = baseUri;
                        }
                        // Configurar timeout según modo desarrollo
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        if (devMode?.Enabled == true && devMode.DisableHttpTimeouts)
                        {
                            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                        }
                        else
                        {
                            client.Timeout = TimeSpan.FromSeconds(30);
                        }
                    });

                    // Registrar ClienteService y su HttpClient pipeline con autenticación
                    services.AddHttpClient<IClienteService, ClienteService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                        {
                            client.BaseAddress = baseUri;
                        }
                        // Configurar timeout según modo desarrollo
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        if (devMode?.Enabled == true && devMode.DisableHttpTimeouts)
                        {
                            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                        }
                        else
                        {
                            client.Timeout = TimeSpan.FromSeconds(30);
                        }
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    // Registrar ProveedorService y su HttpClient pipeline con autenticación
                    services.AddHttpClient<IProveedorService, ProveedorService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                        {
                            client.BaseAddress = baseUri;
                        }
                        // Configurar timeout según modo desarrollo
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        if (devMode?.Enabled == true && devMode.DisableHttpTimeouts)
                        {
                            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                        }
                        else
                        {
                            client.Timeout = TimeSpan.FromSeconds(30);
                        }
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    // Registrar EquipoService y su HttpClient pipeline con autenticación
                    services.AddHttpClient<IEquipoService, EquipoService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                        {
                            client.BaseAddress = baseUri;
                        }
                        // Configurar timeout según modo desarrollo
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        if (devMode?.Enabled == true && devMode.DisableHttpTimeouts)
                        {
                            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                        }
                        else
                        {
                            client.Timeout = TimeSpan.FromSeconds(30);
                        }
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    // Registrar UserInfoService y su HttpClient pipeline con autenticación
                    services.AddHttpClient<IUserInfoService, UserInfoService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                        {
                            client.BaseAddress = baseUri;
                        }
                        // Configurar timeout según modo desarrollo
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        if (devMode?.Enabled == true && devMode.DisableHttpTimeouts)
                        {
                            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                        }
                        else
                        {
                            client.Timeout = TimeSpan.FromSeconds(30);
                        }
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    // Registrar RelacionService y su HttpClient pipeline con autenticación
                    services.AddHttpClient<IRelacionService, RelacionService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                        {
                            client.BaseAddress = baseUri;
                        }
                        // Configurar timeout según modo desarrollo
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        if (devMode?.Enabled == true && devMode.DisableHttpTimeouts)
                        {
                            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                        }
                        else
                        {
                            client.Timeout = TimeSpan.FromSeconds(30);
                        }
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    // Registrar MantenimientoService y su HttpClient pipeline con autenticación
                    services.AddHttpClient<IMantenimientoService, MantenimientoService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                        {
                            client.BaseAddress = baseUri;
                        }
                        // Configurar timeout según modo desarrollo
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        if (devMode?.Enabled == true && devMode.DisableHttpTimeouts)
                        {
                            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                        }
                        else
                        {
                            client.Timeout = TimeSpan.FromSeconds(30);
                        }
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    // Registrar OperacionService y su HttpClient pipeline con autenticación
                    services.AddHttpClient<IOperacionService, OperacionService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                        {
                            client.BaseAddress = baseUri;
                        }
                        // Configurar timeout según modo desarrollo
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        if (devMode?.Enabled == true && devMode.DisableHttpTimeouts)
                        {
                            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                        }
                        else
                        {
                            client.Timeout = TimeSpan.FromSeconds(30);
                        }
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    // Registrar CargoService y su HttpClient pipeline con autenticación
                    services.AddHttpClient<ICargoService, CargoService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                        {
                            client.BaseAddress = baseUri;
                        }
                        // Configurar timeout según modo desarrollo
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        if (devMode?.Enabled == true && devMode.DisableHttpTimeouts)
                        {
                            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                        }
                        else
                        {
                            client.Timeout = TimeSpan.FromSeconds(30);
                        }
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    // Registrar RefaccionService y su HttpClient pipeline con autenticación
                    services.AddHttpClient<IRefaccionService, RefaccionService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                        {
                            client.BaseAddress = baseUri;
                        }
                        // Configurar timeout según modo desarrollo
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        if (devMode?.Enabled == true && devMode.DisableHttpTimeouts)
                        {
                            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                        }
                        else
                        {
                            client.Timeout = TimeSpan.FromSeconds(30);
                        }
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    // Registrar ServicioService y su HttpClient pipeline con autenticación
                    services.AddHttpClient<IServicioService, ServicioService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                        {
                            client.BaseAddress = baseUri;
                        }
                        // Configurar timeout según modo desarrollo
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        if (devMode?.Enabled == true && devMode.DisableHttpTimeouts)
                        {
                            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                        }
                        else
                        {
                            client.Timeout = TimeSpan.FromSeconds(30);
                        }
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    // Registrar RelacionRefaccionEquipoService y su HttpClient pipeline con autenticación
                    services.AddHttpClient<IRelacionRefaccionEquipoService, RelacionRefaccionEquipoService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                        {
                            client.BaseAddress = baseUri;
                        }
                        // Configurar timeout según modo desarrollo
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        if (devMode?.Enabled == true && devMode.DisableHttpTimeouts)
                        {
                            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                        }
                        else
                        {
                            client.Timeout = TimeSpan.FromSeconds(30);
                        }
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    // Registrar RelacionProveedorRefaccionService y su HttpClient pipeline con autenticación
                    services.AddHttpClient<Services.RelacionesProveedorRefaccion.IRelacionProveedorRefaccionService, Services.RelacionesProveedorRefaccion.RelacionProveedorRefaccionService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                        {
                            client.BaseAddress = baseUri;
                        }
                        // Configurar timeout según modo desarrollo
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        if (devMode?.Enabled == true && devMode.DisableHttpTimeouts)
                        {
                            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                        }
                        else
                        {
                            client.Timeout = TimeSpan.FromSeconds(30);
                        }
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    // Registrar NavigationService
                    services.AddSingleton<INavigationService, NavigationService>();

                    // Registrar DialogService
                    services.AddSingleton<IDialogService, DialogService>();

                    // Registrar NotificacionService
                    services.AddSingleton<INotificacionService, NotificacionService>();

                    // Registrar QuoteService (PDF generation)
                    services.AddSingleton<IQuoteService, QuoteService>();

                    // Registrar GoogleMapsConfigService y su HttpClient pipeline con autenticación
                    services.AddHttpClient<IGoogleMapsConfigService, GoogleMapsConfigService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                        {
                            client.BaseAddress = baseUri;
                        }
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        if (devMode?.Enabled == true && devMode.DisableHttpTimeouts)
                        {
                            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                        }
                        else
                        {
                            client.Timeout = TimeSpan.FromSeconds(30);
                        }
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    // Registrar AreasService y su HttpClient pipeline con autenticación
                    services.AddHttpClient<IAreasService, AreasService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                        {
                            client.BaseAddress = baseUri;
                        }
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        if (devMode?.Enabled == true && devMode.DisableHttpTimeouts)
                        {
                            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                        }
                        else
                        {
                            client.Timeout = TimeSpan.FromSeconds(30);
                        }
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    // Registrar ViewModels
                    services.AddTransient<ViewModels.MainViewModel>();
                    services.AddTransient<ViewModels.LoginViewModel>();
                    services.AddTransient<ViewModels.CustomersViewModel>();
                    services.AddTransient<ViewModels.ProveedoresViewModel>();
                    services.AddTransient<ViewModels.EquiposViewModel>();
                    services.AddTransient<ViewModels.OperacionesViewModel>();
                    services.AddTransient<ViewModels.AcesoriaViewModel>();
                    services.AddTransient<ViewModels.MttoViewModel>();
                    services.AddTransient<ViewModels.NuevoEquipoViewModel>();
                    services.AddTransient<ViewModels.RefaccionesViewModel>();
                    services.AddTransient<ViewModels.ServiciosViewModel>();
                    services.AddTransient<ViewModels.UbicacionesViewModel>();

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
            
            // Guardar la referencia a la ventana principal para acceder a XamlRoot desde DialogService
            MainWindow = window;

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