using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
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
using Advance_Control.Services.Contactos;
using Advance_Control.Services.Equipos;
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.UserInfo;
using Advance_Control.Services.Session;
using Advance_Control.Services.Relaciones;
using Advance_Control.Services.OrdenServicio;
using Advance_Control.Services.Refacciones;
using Advance_Control.Services.RelacionesRefaccionEquipo;
using Advance_Control.Services.Proveedores;
using Advance_Control.Services.Operaciones;
using Advance_Control.Services.Cargos;
using Advance_Control.Services.Servicios;
using Advance_Control.Services.Quotes;
using Advance_Control.Services.Reportes;
using Advance_Control.Services.GoogleMaps;
using Advance_Control.Services.Areas;
using Advance_Control.Services.Ubicaciones;
using Advance_Control.Services.LocalStorage;
using Advance_Control.Services.Entidades;
using Advance_Control.Services.ImageViewer;
using Advance_Control.Services.CorreoUsuario;
using Advance_Control.Services.TipoUsuario;
using Advance_Control.Services.PermisosUi;
using Advance_Control.Services.UsuariosAdmin;

namespace Advance_Control
{
    public partial class App : Application
    {
        public IHost Host { get; }
        private static readonly string PackagedConfigurationFile = Path.Combine(
            AppContext.BaseDirectory,
            "appsettings.json");
        private static readonly string ExternalConfigurationDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Advance Control");
        private static readonly string ExternalConfigurationFile = Path.Combine(
            ExternalConfigurationDirectory,
            "appsettings.local.json");
        
        // Almacena la referencia a la ventana principal para acceder a XamlRoot
        public static Window? MainWindow { get; private set; }

        private static void EnsureExternalConfigurationFile()
        {
            Directory.CreateDirectory(ExternalConfigurationDirectory);

            if (File.Exists(ExternalConfigurationFile))
            {
                return;
            }

            var configuration = new JsonObject
            {
                ["ExternalApi"] = new JsonObject
                {
                    ["BaseUrl"] = GetDefaultApiBaseUrl(),
                    ["ProductionUrl"] = GetDefaultApiProductionUrl()
                }
            };

            File.WriteAllText(
                ExternalConfigurationFile,
                configuration.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        private static string GetDefaultApiBaseUrl()
        {
            if (!File.Exists(PackagedConfigurationFile))
            {
                return "https://localhost:7055/";
            }

            using var stream = File.OpenRead(PackagedConfigurationFile);
            using var document = JsonDocument.Parse(stream);

            if (document.RootElement.TryGetProperty("ExternalApi", out var externalApiSection)
                && externalApiSection.TryGetProperty("BaseUrl", out var baseUrlElement)
                && baseUrlElement.ValueKind == JsonValueKind.String)
            {
                var configuredBaseUrl = baseUrlElement.GetString();
                if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
                {
                    return configuredBaseUrl.Trim();
                }
            }

            return "https://localhost:7055/";
        }

        private static string GetDefaultApiProductionUrl()
        {
            if (!File.Exists(PackagedConfigurationFile))
            {
                return "https://advance-elevadores.mx/";
            }

            using var stream = File.OpenRead(PackagedConfigurationFile);
            using var document = JsonDocument.Parse(stream);

            if (document.RootElement.TryGetProperty("ExternalApi", out var externalApiSection)
                && externalApiSection.TryGetProperty("ProductionUrl", out var urlElement)
                && urlElement.ValueKind == JsonValueKind.String)
            {
                var configuredUrl = urlElement.GetString();
                if (!string.IsNullOrWhiteSpace(configuredUrl))
                {
                    return configuredUrl.Trim();
                }
            }

            return "https://advance-elevadores.mx/";
        }

        public App()
        {
            this.InitializeComponent();
            EnsureExternalConfigurationFile();

            Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(cfg =>
                {
                    cfg.AddJsonFile(PackagedConfigurationFile, optional: true, reloadOnChange: true);
                    cfg.AddJsonFile(ExternalConfigurationFile, optional: true, reloadOnChange: true);
                    cfg.AddEnvironmentVariables(prefix: "ADVANCECONTROL_");
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
                            client.Timeout = TimeSpan.FromSeconds(15);
                        }
                    });

                    // Registrar implementación de almacenamiento seguro (Windows PasswordVault)
                    services.AddSingleton<ISecureStorage, SecretStorageWindows>();

                    // Registrar Lazy<IUserSessionService> para romper dependencia circular
                    // LoggingService necesita la sesión pero UserSessionService necesita al LoggingService
                    services.AddSingleton(sp =>
                        new Lazy<Services.Session.IUserSessionService>(
                            () => sp.GetRequiredService<Services.Session.IUserSessionService>()));

                    // Registrar AuthenticatedHttpHandler con Lazy<IAuthService> para romper dependencia circular
                    services.AddTransient<Services.Http.AuthenticatedHttpHandler>(sp =>
                    {
                        var lazyAuthService = new Lazy<IAuthService>(() => sp.GetRequiredService<IAuthService>());
                        var endpointProvider = sp.GetRequiredService<IApiEndpointProvider>();
                        var logger = sp.GetService<ILoggingService>(); // optional
                        return new Services.Http.AuthenticatedHttpHandler(lazyAuthService, endpointProvider, logger);
                    });

                    // Registrar cliente HTTP nombrado para LoggingService
                    services.AddHttpClient("LoggingService", (sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                            client.BaseAddress = baseUri;
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        client.Timeout = devMode?.Enabled == true && devMode.DisableHttpTimeouts
                            ? System.Threading.Timeout.InfiniteTimeSpan
                            : TimeSpan.FromSeconds(15);
                    });

                    // Singleton: una sola instancia para toda la vida de la app.
                    // LoggingService no tiene estado de sesión propio, solo usa HttpClient y Lazy<IUserSessionService>.
                    services.AddSingleton<ILoggingService>(sp =>
                    {
                        var factory = sp.GetRequiredService<IHttpClientFactory>();
                        var http = factory.CreateClient("LoggingService");
                        var endpoints = sp.GetRequiredService<IApiEndpointProvider>();
                        var session = sp.GetRequiredService<Lazy<Services.Session.IUserSessionService>>();
                        var navigation = sp.GetRequiredService<INavigationService>();
                        return new LoggingService(http, endpoints, session, navigation);
                    });

                    // Registrar cliente HTTP nombrado para AuthService (sin AuthenticatedHttpHandler
                    // para evitar dependencia circular: Auth no necesita token Bearer para login/refresh)
                    services.AddHttpClient("AuthService", (sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                            client.BaseAddress = baseUri;
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        client.Timeout = devMode?.Enabled == true && devMode.DisableHttpTimeouts
                            ? System.Threading.Timeout.InfiniteTimeSpan
                            : TimeSpan.FromSeconds(30);
                    });

                    // SINGLETON CRÍTICO: una sola instancia de AuthService en toda la app.
                    // Si fuera Transient (AddHttpClient<IAuthService, AuthService>), cada handler
                    // crearía su propia instancia con su propio estado de tokens. Cuando varias instancias
                    // intentan refrescar el mismo refresh token simultáneamente, la API detecta
                    // "token reutilizado" y revoca todas las sesiones, causando logout inesperado.
                    services.AddSingleton<IAuthService>(sp =>
                    {
                        var factory = sp.GetRequiredService<IHttpClientFactory>();
                        var http = factory.CreateClient("AuthService");
                        var endpoints = sp.GetRequiredService<IApiEndpointProvider>();
                        var storage = sp.GetRequiredService<ISecureStorage>();
                        var logger = sp.GetRequiredService<ILoggingService>();
                        var devMode = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>();
                        return new AuthService(http, endpoints, storage, logger, devMode);
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

                    // Singleton de sesión de usuario: se carga una vez tras el login y está disponible en toda la app
                    services.AddSingleton<IUserSessionService, UserSessionService>();

                    // Registrar RelacionServicey su HttpClient pipeline con autenticación
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

                    // Registrar RelacionUsuarioAreaService y su HttpClient pipeline con autenticación
                    services.AddHttpClient<Services.RelacionUsuarioArea.IRelacionUsuarioAreaService, Services.RelacionUsuarioArea.RelacionUsuarioAreaService>((sp, client) =>
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

                    // Registrar OrdenServicioService y su HttpClient pipeline con autenticación
                    services.AddHttpClient<IOrdenServicioService, OrdenServicioService>((sp, client) =>
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

                    // Registrar CheckOperacionService
                    services.AddHttpClient<Services.CheckOperacion.ICheckOperacionService, Services.CheckOperacion.CheckOperacionService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                            client.BaseAddress = baseUri;
                        client.Timeout = TimeSpan.FromSeconds(15);
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
                    services.AddSingleton<Services.Conciliacion.IConciliacionRulesProvider, Services.Conciliacion.ConciliacionRulesProvider>();
                    services.AddSingleton<Services.Conciliacion.ConciliacionMatchingEngine>();

                    // Registrar FirmaService y QuoteService (PDF generation)
                    // Registrar cliente HTTP nombrado para RemoteFirmaService (Singleton con IHttpClientFactory)
                    services.AddHttpClient("RemoteFirmas", (sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                            client.BaseAddress = baseUri;
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        client.Timeout = devMode?.Enabled == true && devMode.DisableHttpTimeouts
                            ? System.Threading.Timeout.InfiniteTimeSpan
                            : TimeSpan.FromSeconds(60);
                    }).AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    services.AddSingleton<IFirmaService>(sp =>
                    {
                        var factory = sp.GetRequiredService<IHttpClientFactory>();
                        var http = factory.CreateClient("RemoteFirmas");
                        var logger = sp.GetRequiredService<ILoggingService>();
                        return new Services.Quotes.RemoteFirmaService(http, logger);
                    });
                    services.AddSingleton<IQuoteService, QuoteService>();
                    services.AddSingleton<Services.Reportes.IReporteFinancieroFacturacionExportService, Services.Reportes.ReporteFinancieroFacturacionExportService>();

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

                    // Registrar UbicacionService y su HttpClient pipeline con autenticación
                    services.AddHttpClient<IUbicacionService, UbicacionService>((sp, client) =>
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

                    // Registrar EntidadService y su HttpClient pipeline con autenticación
                    services.AddHttpClient<IEntidadService, EntidadService>((sp, client) =>
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

                    // Registrar ContactoService y su HttpClient pipeline con autenticación
                    services.AddHttpClient<IContactoService, ContactoService>((sp, client) =>
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

                    services.AddHttpClient<Services.EstadoCuenta.IEstadoCuentaXmlService, Services.EstadoCuenta.EstadoCuentaXmlService>((sp, client) =>
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
                            client.Timeout = TimeSpan.FromSeconds(60);
                        }
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    services.AddHttpClient<Services.Facturas.IFacturaService, Services.Facturas.FacturaService>((sp, client) =>
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
                            client.Timeout = TimeSpan.FromSeconds(60);
                        }
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    services.AddHttpClient<Services.Reportes.IReporteFinancieroFacturacionService, Services.Reportes.ReporteFinancieroFacturacionService>((sp, client) =>
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
                            client.Timeout = TimeSpan.FromSeconds(60);
                        }
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    // Registrar LocalCargoImageService para almacenamiento local de imágenes de cargos
                    services.AddHttpClient("RemoteCargos", (sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                            client.BaseAddress = baseUri;
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        client.Timeout = devMode?.Enabled == true && devMode.DisableHttpTimeouts
                            ? System.Threading.Timeout.InfiniteTimeSpan
                            : TimeSpan.FromSeconds(120);
                    }).AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    services.AddSingleton<ICargoImageService>(sp =>
                    {
                        var factory = sp.GetRequiredService<IHttpClientFactory>();
                        var http = factory.CreateClient("RemoteCargos");
                        var logger = sp.GetRequiredService<ILoggingService>();
                        return new Services.LocalStorage.RemoteCargoImageService(http, logger);
                    });

                    // Registrar servicio de imágenes para levantamiento
                    services.AddHttpClient("RemoteLevantamientos", (sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                            client.BaseAddress = baseUri;
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        client.Timeout = devMode?.Enabled == true && devMode.DisableHttpTimeouts
                            ? System.Threading.Timeout.InfiniteTimeSpan
                            : TimeSpan.FromSeconds(120);
                    }).AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    services.AddSingleton<ILevantamientoImageService>(sp =>
                    {
                        var factory = sp.GetRequiredService<IHttpClientFactory>();
                        var http = factory.CreateClient("RemoteLevantamientos");
                        var logger = sp.GetRequiredService<ILoggingService>();
                        return new Services.LocalStorage.RemoteLevantamientoImageService(http, logger);
                    });

                    // Registrar servicio de reporte PDF para levantamiento
                    services.AddSingleton<ILevantamientoReportService, LevantamientoReportService>();

                    // Registrar LocalOperacionImageService para almacenamiento local de imágenes de operaciones (Prefacturas y Órdenes de Compra)
                    services.AddHttpClient("RemoteOperaciones", (sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                            client.BaseAddress = baseUri;
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        client.Timeout = devMode?.Enabled == true && devMode.DisableHttpTimeouts
                            ? System.Threading.Timeout.InfiniteTimeSpan
                            : TimeSpan.FromSeconds(120);
                    }).AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    services.AddSingleton<IOperacionImageService>(sp =>
                    {
                        var factory = sp.GetRequiredService<IHttpClientFactory>();
                        var http = factory.CreateClient("RemoteOperaciones");
                        var logger = sp.GetRequiredService<ILoggingService>();
                        return new Services.LocalStorage.RemoteOperacionImageService(http, logger);
                    });

                    // Registrar ImageViewerService para el visor de imágenes reutilizable
                    services.AddSingleton<IImageViewerService, ImageViewerService>();

                    // Registrar ViewModels
                    services.AddTransient<ViewModels.MainViewModel>();
                    services.AddTransient<ViewModels.LoginViewModel>();
                    services.AddTransient<ViewModels.CustomersViewModel>();
                    services.AddTransient<ViewModels.ProveedoresViewModel>();
                    services.AddTransient<ViewModels.EquiposViewModel>();
                    services.AddTransient<ViewModels.OperacionesViewModel>();
                    services.AddTransient<ViewModels.AcesoriaViewModel>();
                    services.AddTransient<ViewModels.OrdenServicioViewModel>();
                    services.AddTransient<ViewModels.LevantamientoViewModel>();
                    services.AddTransient<ViewModels.LevantamientosViewModel>();
                    services.AddTransient<ViewModels.NuevoEquipoViewModel>();
                    services.AddTransient<ViewModels.RefaccionesViewModel>();
                    services.AddTransient<ViewModels.ServiciosViewModel>();
                    services.AddTransient<ViewModels.UbicacionesViewModel>();
                    services.AddTransient<ViewModels.AreasViewModel>();
                    services.AddTransient<ViewModels.EntidadesViewModel>();
                    services.AddTransient<ViewModels.ContactosViewModel>();
                    services.AddTransient<ViewModels.DashboardViewModel>();
                    services.AddTransient<ViewModels.EsCuentaViewModel>();
                    services.AddTransient<ViewModels.DetailEstadoCuentaViewModel>();
                    services.AddTransient<ViewModels.ConciliacionViewModel>();
                    services.AddTransient<ViewModels.ConciliacionAutomaticaWindowViewModel>();
                    services.AddTransient<ViewModels.UsuariosAdminViewModel>();
                    services.AddTransient<ViewModels.FacturasViewModel>();
                    services.AddTransient<ViewModels.DetailFacturaViewModel>();
                    services.AddTransient<ViewModels.RPTFinancieroFacturacionViewModel>();
                    services.AddTransient<ViewModels.DevOpsViewModel>();

                    services.AddHttpClient<ICorreoUsuarioService, CorreoUsuarioService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                            client.BaseAddress = baseUri;
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        client.Timeout = devMode?.Enabled == true && devMode.DisableHttpTimeouts
                            ? System.Threading.Timeout.InfiniteTimeSpan
                            : TimeSpan.FromSeconds(30);
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    services.AddSingleton<Services.Email.IEmailService, Services.Email.EmailService>();
                    services.AddSingleton<Services.Theme.IThemeService, Services.Theme.ThemeService>();
                    services.AddTransient<ViewModels.CorreoViewModel>();

                    // Registrar ActivityService para el dashboard
                    services.AddHttpClient<Services.Activity.IActivityService, Services.Activity.ActivityService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                            client.BaseAddress = baseUri;
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    // Registrar DashboardService para conteos del dashboard
                    services.AddHttpClient<Services.Dashboard.IDashboardService, Services.Dashboard.DashboardService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                            client.BaseAddress = baseUri;
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    // Registrar LevantamientoApiService con autenticación
                    services.AddHttpClient<Services.Levantamiento.ILevantamientoApiService, Services.Levantamiento.LevantamientoApiService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                            client.BaseAddress = baseUri;
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    // Registrar NivelService con autenticación
                    services.AddHttpClient<Services.Nivel.INivelService, Services.Nivel.NivelService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                            client.BaseAddress = baseUri;
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        client.Timeout = devMode?.Enabled == true && devMode.DisableHttpTimeouts
                            ? System.Threading.Timeout.InfiniteTimeSpan
                            : TimeSpan.FromSeconds(30);
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    services.AddHttpClient<ITipoUsuarioService, TipoUsuarioService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                            client.BaseAddress = baseUri;
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        client.Timeout = devMode?.Enabled == true && devMode.DisableHttpTimeouts
                            ? System.Threading.Timeout.InfiniteTimeSpan
                            : TimeSpan.FromSeconds(30);
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    services.AddHttpClient<Services.TipoMantenimiento.ITipoMantenimientoService, Services.TipoMantenimiento.TipoMantenimientoService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                            client.BaseAddress = baseUri;
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        client.Timeout = devMode?.Enabled == true && devMode.DisableHttpTimeouts
                            ? System.Threading.Timeout.InfiniteTimeSpan
                            : TimeSpan.FromSeconds(30);
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    services.AddHttpClient<IPermisoUiService, PermisoUiService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                            client.BaseAddress = baseUri;
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        client.Timeout = devMode?.Enabled == true && devMode.DisableHttpTimeouts
                            ? System.Threading.Timeout.InfiniteTimeSpan
                            : TimeSpan.FromSeconds(30);
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    services.AddSingleton<IPermisoUiScanner, PermisoUiScanner>();
                    services.AddSingleton<IPermisoUiRuntimeService, PermisoUiRuntimeService>();

                    // Registrar UsuarioAdminService con autenticación
                    services.AddHttpClient<IUsuarioAdminService, UsuarioAdminService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                            client.BaseAddress = baseUri;
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        client.Timeout = devMode?.Enabled == true && devMode.DisableHttpTimeouts
                            ? System.Threading.Timeout.InfiniteTimeSpan
                            : TimeSpan.FromSeconds(30);
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    // Singleton de control de acceso por nivel
                    services.AddSingleton<Services.AccessControl.IAccessControlService, Services.AccessControl.AccessControlService>();

                    // Registrar DevOpsService con autenticación
                    services.AddHttpClient<Services.DevOps.IDevOpsService, Services.DevOps.DevOpsService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                            client.BaseAddress = baseUri;
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        client.Timeout = devMode?.Enabled == true && devMode.DisableHttpTimeouts
                            ? System.Threading.Timeout.InfiniteTimeSpan
                            : TimeSpan.FromSeconds(60);
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    // Registrar NotificacionAlertaService (alertas inteligentes persistentes en BD)
                    services.AddHttpClient<Services.Alertas.INotificacionAlertaService, Services.Alertas.NotificacionAlertaService>((sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                            client.BaseAddress = baseUri;
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        client.Timeout = devMode?.Enabled == true && devMode.DisableHttpTimeouts
                            ? System.Threading.Timeout.InfiniteTimeSpan
                            : TimeSpan.FromSeconds(30);
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    // Registrar MensajeriaService (chat + SignalR en tiempo real)
                    services.AddSingleton<Services.Mensajeria.IMensajeriaService, Services.Mensajeria.MensajeriaService>(sp =>
                    {
                        var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                        var httpClient = httpFactory.CreateClient("MensajeriaRest");
                        var endpoints = sp.GetRequiredService<IApiEndpointProvider>();
                        var logger = sp.GetRequiredService<ILoggingService>();
                        return new Services.Mensajeria.MensajeriaService(httpClient, endpoints, logger);
                    });
                    services.AddHttpClient("MensajeriaRest", (sp, client) =>
                    {
                        var provider = sp.GetRequiredService<IApiEndpointProvider>();
                        if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
                            client.BaseAddress = baseUri;
                        var devMode = sp.GetService<Microsoft.Extensions.Options.IOptions<Settings.DevelopmentModeOptions>>()?.Value;
                        client.Timeout = devMode?.Enabled == true && devMode.DisableHttpTimeouts
                            ? System.Threading.Timeout.InfiniteTimeSpan
                            : TimeSpan.FromSeconds(30);
                    })
                    .AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();

                    // Registrar MensajesViewModel
                    services.AddTransient<ViewModels.MensajesViewModel>();

                    // Registrar ChatPanelViewModel (Singleton — vive mientras la app está abierta)
                    services.AddSingleton<ViewModels.ChatPanelViewModel>();

                    // Registrar MainWindow para que DI pueda resolverlo y proporcionar sus dependencias
                    services.AddTransient<MainWindow>();
                })
                .Build();
        }

        public static string GetExternalConfigurationDirectory() => ExternalConfigurationDirectory;

        public static string GetExternalConfigurationFile() => ExternalConfigurationFile;

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // Registrar AppNotificationManager para notificaciones de Windows.
            // El handler debe registrarse ANTES de Register() para interceptar activaciones por clic.
            try
            {
                AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
                AppNotificationManager.Default.Register();
            }
            catch (Exception)
            {
                // App corriendo sin identidad MSIX (ej. debug directo): las notificaciones no estarán disponibles.
            }

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
                    AppNotificationManager.Default.Unregister();
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

            // Capturar excepciones no controladas para diagnóstico (muestra el error real en vez de crash silencioso)
            this.UnhandledException += async (sender, args) =>
            {
                args.Handled = true;
                var ex = args.Exception;
                var detalle = $"Tipo: {ex?.GetType().FullName}\n\nMensaje: {ex?.Message}\n\nStackTrace:\n{ex?.StackTrace}";
                EscribirCrashLog("UI.UnhandledException", detalle);
                System.Diagnostics.Debug.WriteLine($"[CRASH CAPTURADO]\n{detalle}");

                try
                {
                    var xamlRoot = MainWindow?.Content?.XamlRoot;
                    if (xamlRoot != null)
                    {
                        var scroll = new Microsoft.UI.Xaml.Controls.ScrollViewer
                        {
                            MaxHeight = 400,
                            Content = new Microsoft.UI.Xaml.Controls.TextBlock
                            {
                                Text = detalle,
                                IsTextSelectionEnabled = true,
                                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                                FontSize = 11
                            }
                        };
                        var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                        {
                            Title = "⚠ Error no controlado (diagnóstico)",
                            Content = scroll,
                            CloseButtonText = "Cerrar",
                            XamlRoot = xamlRoot
                        };
                        await dialog.ShowAsync();
                    }
                }
                catch { /* Si el dialog falla, al menos tenemos el output de Debug */ }
            };

            // Capturar excepciones de threads no-UI (background threads, finalizers)
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try
                {
                    var ex = e.ExceptionObject as Exception;
                    EscribirCrashLog("AppDomain.UnhandledException",
                        $"Tipo: {ex?.GetType().FullName}\n\nMensaje: {ex?.Message}\n\nStackTrace:\n{ex?.StackTrace}\n\nIsTerminating: {e.IsTerminating}");
                }
                catch { }
            };

            // Capturar tasks fire-and-forget que fallan sin observar
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                try
                {
                    EscribirCrashLog("TaskScheduler.UnobservedTaskException",
                        $"Tipo: {e.Exception?.GetType().FullName}\n\nMensaje: {e.Exception?.Message}\n\nStackTrace:\n{e.Exception?.StackTrace}");
                    e.SetObserved();
                }
                catch { }
            };

            window.Activate();
        }

        /// <summary>Escribe un log de crash al archivo %TEMP%\advancecontrol_crash.log para diagnóstico cuando el diálogo falla.</summary>
        private static void EscribirCrashLog(string fuente, string detalle)
        {
            try
            {
                var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "advancecontrol_crash.log");
                var entrada = $"\n========== {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {fuente} ==========\n{detalle}\n";
                System.IO.File.AppendAllText(path, entrada);
            }
            catch { /* Si ni el log a archivo funciona, no podemos hacer más */ }
        }

        /// <summary>
        /// Maneja el clic en una notificación de Windows: trae la ventana existente al frente
        /// y abre el panel de chat con el usuario correspondiente.
        /// </summary>
        private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
        {
            // Leer credencialId de los argumentos de la notificación
            long credencialId = 0;
            try
            {
                if (args.Arguments.TryGetValue("credencialId", out var idStr))
                    long.TryParse(idStr, out credencialId);
            }
            catch { }

            // Fallback: usar el último ID guardado por ChatPanelViewModel
            if (credencialId <= 0)
                credencialId = ViewModels.ChatPanelViewModel.UltimaNotificacionCredencialId;

            System.Diagnostics.Debug.WriteLine($"[Notificacion] credencialId={credencialId}, raw='{args.Argument}'");

            MainWindow?.DispatcherQueue?.TryEnqueue(() =>
            {
                if (MainWindow is null) return;

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow);

                if (IsIconic(hwnd))
                    ShowWindow(hwnd, 9); // SW_RESTORE

                SetForegroundWindow(hwnd);

                if (MainWindow is MainWindow mw)
                {
                    mw.MostrarChatPanel();

                    if (credencialId > 0)
                        mw.AbrirChatConUsuario(credencialId);
                }
            });
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
    }
}
