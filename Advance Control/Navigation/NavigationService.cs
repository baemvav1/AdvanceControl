using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.Navigation
{
    public class NavigationService : INavigationService
    {
        private Frame _frame;
        private readonly Dictionary<string, RouteEntry> _routes = new(StringComparer.OrdinalIgnoreCase);
        private string _currentTag;

        private record RouteEntry(Type PageType, Func<object> Factory);

        public void Initialize(Frame frame)
        {
            _frame = frame ?? throw new ArgumentNullException(nameof(frame));
            // Opcional: suscribirse a eventos del frame para mantener estado
            _frame.Navigated += Frame_Navigated;
        }

        private void Frame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            // Actualiza el tag actual si navegamos por Type
            try
            {
                if (e?.SourcePageType != null)
                {
                    foreach (var kv in _routes)
                    {
                        if (kv.Value.PageType == e.SourcePageType)
                        {
                            _currentTag = kv.Key;
                            return;
                        }
                    }
                }
            }
            catch { /* no crítico */ }
        }

        /// <summary>
        /// Registra una ruta asociando un tag con un Type de página.
        /// Recomendado para que Frame gestione el BackStack correctamente.
        /// </summary>
        public void Configure(string tag, Type pageType)
        {
            if (string.IsNullOrWhiteSpace(tag)) throw new ArgumentNullException(nameof(tag));
            if (pageType == null) throw new ArgumentNullException(nameof(pageType));
            if (!typeof(Page).IsAssignableFrom(pageType))
                throw new ArgumentException("pageType debe heredar de Microsoft.UI.Xaml.Controls.Page", nameof(pageType));

            _routes[tag] = new RouteEntry(pageType, null);
        }

        /// <summary>
        /// Versión genérica de Configure para mayor comodidad.
        /// </summary>
        public void Configure<TPage>(string tag) where TPage : Page
        {
            Configure(tag, typeof(TPage));
        }

        /// <summary>
        /// Registra una factory que devuelve algo asociado a la ruta:
        /// - Si la factory devuelve una Type (Type), se usará Frame.Navigate(Type, parameter).
        /// - Si la factory devuelve una Page (instancia), se asignará a Frame.Content (NO se gestiona BackStack).
        /// - Si la factory devuelve otra cosa, se intentará navegar si es Type, en otro caso fallará.
        /// 
        /// Uso típico con DI:
        ///   navigationService.ConfigureFactory("MyPage", () => serviceProvider.GetRequiredService<MyPage>());
        /// 
        /// Nota: Usar factory que devuelva Type es preferible si quieres que Frame gestione el BackStack.
        /// </summary>
        public void ConfigureFactory(string tag, Func<object> factory)
        {
            if (string.IsNullOrWhiteSpace(tag)) throw new ArgumentNullException(nameof(tag));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            _routes[tag] = new RouteEntry(null, factory);
        }

        /// <summary>
        /// Navega a la ruta identificada por 'tag'.
        /// Devuelve true si la navegación se inició con éxito.
        /// </summary>
        public bool Navigate(string tag, object parameter = null)
        {
            if (_frame == null) throw new InvalidOperationException("NavigationService no inicializado. Llame a Initialize(frame) primero.");
            if (string.IsNullOrWhiteSpace(tag)) return false;

            if (!_routes.TryGetValue(tag, out var entry))
            {
                Debug.WriteLine($"NavigationService: no existe ruta para tag '{tag}'");
                return false;
            }

            // Si la entrada tiene una factory definida -> resolverla
            if (entry.Factory != null)
            {
                object result;
                try
                {
                    result = entry.Factory();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"NavigationService: la factory para '{tag}' lanzó una excepción: {ex}");
                    return false;
                }

                if (result == null)
                {
                    Debug.WriteLine($"NavigationService: la factory para '{tag}' devolvió null.");
                    return false;
                }

                // Si la factory devolvió un Type -> usar Frame.Navigate(Type)
                if (result is Type resultType && typeof(Page).IsAssignableFrom(resultType))
                {
                    var navigated = _frame.Navigate(resultType, parameter);
                    if (navigated) _currentTag = tag;
                    return navigated;
                }

                // Si la factory devolvió una Page (instancia) -> asignarla a Content.
                if (result is Page pageInstance)
                {
                    // Aviso: asignar Content no actualiza el BackStack del Frame.
                    _frame.Content = pageInstance;
                    _currentTag = tag;
                    return true;
                }

                Debug.WriteLine($"NavigationService: la factory para '{tag}' devolvió un objeto inesperado del tipo {result.GetType().FullName}.");
                return false;
            }

            // Si la entrada tiene un PageType -> usar Frame.Navigate(Type)
            if (entry.PageType != null)
            {
                var navigated = _frame.Navigate(entry.PageType, parameter);
                if (navigated) _currentTag = tag;
                return navigated;
            }

            return false;
        }

        public bool CanGoBack => _frame != null && _frame.CanGoBack;

        public void GoBack()
        {
            if (_frame == null) throw new InvalidOperationException("NavigationService no inicializado.");
            if (_frame.CanGoBack) _frame.GoBack();
        }

        public string GetCurrentTag() => _currentTag;
    }

    /* -----------------------------
       SECCIÓN DOCUMENTADA: Cómo referenciar vistas (XAML + .xaml.cs)
       -----------------------------
       Cada vista/página en WinUI se compone típicamente de dos archivos:
         - MyPage.xaml       <- define la UI y lleva x:Class="Mi.Namespace.MyPage"
         - MyPage.xaml.cs    <- clase parcial con constructor e InitializeComponent()

       Ejemplo mínimo de página:
       ---------------------------------------
       // MyPage.xaml
       <Page
         x:Class="Advance_Control.Navigation.Pages.SamplePage1"
         xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
           <Grid>
             <TextBlock Text="Hola desde SamplePage1" />
           </Grid>
       </Page>

       // MyPage.xaml.cs
       namespace Advance_Control.Navigation.Pages
       {
           public sealed partial class SamplePage1 : Microsoft.UI.Xaml.Controls.Page
           {
               public SamplePage1()
               {
                   this.InitializeComponent();
                   // Si usas DI para ViewModel:
                   // DataContext = App.Current.Services.GetRequiredService<SamplePage1ViewModel>();
               }
           }
       }

       Cómo registrar la página en NavigationService (opciones):
       1) Registro por Type (recomendado):
          // en el constructor donde tienes acceso a navigationService:
          navigationService.Configure("SamplePage1", typeof(SamplePage1));
          // o usando la versión genérica:
          navigationService.Configure<SamplePage1>("SamplePage1");

          // Y en el NavigationView Item:
          // <NavigationViewItem Content="Pagina 1" Tag="SamplePage1" ... />
          // Cuando el usuario pulse, en el handler:
          _navigationService.Navigate("SamplePage1");

       2) Registro con factory que devuelve Type:
          // útil si quieres resolver el Type dinámicamente desde DI:
          navigationService.ConfigureFactory("SamplePage1", () => typeof(SamplePage1));
          // Esto hace que el servicio use Frame.Navigate(typeof(SamplePage1), parameter)

       3) Registro con factory que devuelve instancia Page (NO gestiona BackStack automáticamente):
          // Si necesitas forzar creación de instancias desde DI:
          navigationService.ConfigureFactory("SamplePage1", () => serviceProvider.GetRequiredService<SamplePage1>());
          // Al navegar, NavigationService asignará esa instancia a Frame.Content.
          // Limitación: Frame.BackStack NO se actualizará automáticamente con este método.

       Recomendación práctica:
       - Si quieres BackStack y comportamiento estándar de Frame -> usar Configure(tag, typeof(Page)).
       - Si necesitas inyectar ViewModel en la página, registra el ViewModel en DI y dentro del constructor de la Page resuelve el ViewModel desde el ServiceProvider (o pásalo por property).
       - Si quieres instanciar páginas desde DI y aun así conservar BackStack, crea una pequeña página "wrapper" con constructor por defecto que en OnNavigatedTo resuelva los servicios necesarios y cargue el contenido dinámicamente.

       Ejemplo de uso típico en MainWindow:
       ------------------------------------------------
       public MainWindow(..., INavigationService navigationService)
       {
           InitializeComponent();
           _navigationService = navigationService;
           _navigationService.Initialize(contentFrame);
           _navigationService.Configure<SamplePage1>("SamplePage1");
           _navigationService.Configure<SamplePage2>("SamplePage2");
       }

       private void OnNavigationViewItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
       {
           if (args.InvokedItemContainer is NavigationViewItem item)
           {
               var tag = item.Tag?.ToString();
               if (!string.IsNullOrEmpty(tag))
               {
                   _navigationService.Navigate(tag);
               }
           }
       }
    */
}
