using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;
using AdvanceControl.Services;

namespace AdvanceControl
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = default!;

        public App()
        {
            this.InitializeComponent();
            ConfigureServices();
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // Token options: en producción cargar desde settings/secure config
            var tokenOptions = new TokenOptions
            {
                Issuer = "advance",
                Audience = "advance_client",
                SigningKey = "change_this_to_strong_secret_at_runtime_which_is_long_enough",
                ExpiryMinutes = 60
            };

            services.AddSingleton(tokenOptions);
            services.AddSingleton<ITokenService, JwtService>();
            services.AddSingleton<AuthenticationService>();

            // Registrar otros servicios o ViewModels aquí, p.ej MainViewModel
            // services.AddSingleton<IApiService, ApiService>(); // cuando tengas backend
            Services = services.BuildServiceProvider();
        }
    }
}