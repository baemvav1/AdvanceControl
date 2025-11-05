using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Advance_Control.Services.Dialog
{
    /// <summary>
    /// Implementación del servicio de diálogos para WinUI 3.
    /// Permite mostrar UserControls personalizados en diálogos de contenido (ContentDialog).
    /// Cuando no se configuran botones, el diálogo se cierra automáticamente al hacer clic fuera (light dismiss).
    /// </summary>
    /// <remarks>
    /// Este servicio proporciona una forma flexible de mostrar diálogos con UserControls,
    /// permitiendo pasar parámetros de entrada y obtener resultados de salida de forma genérica.
    /// 
    /// <para><b>Características principales:</b></para>
    /// <list type="bullet">
    ///   <item>Soporte para UserControls con o sin parámetros de entrada</item>
    ///   <item>Resultados genéricos de cualquier tipo (bool, string, List, objetos personalizados, etc.)</item>
    ///   <item>Configuración flexible de botones del diálogo</item>
    ///   <item>Light dismiss: se cierra al hacer clic fuera cuando no hay botones configurados</item>
    ///   <item>API fluida y fácil de usar</item>
    /// </list>
    /// 
    /// <para><b>Ejemplos de uso:</b></para>
    /// 
    /// <example>
    /// <code>
    /// // ============================================================================
    /// // EJEMPLO 1: Diálogo simple sin parámetros ni resultado específico
    /// // ============================================================================
    /// // UserControl: SimpleMessageUserControl.xaml.cs
    /// public sealed partial class SimpleMessageUserControl : UserControl
    /// {
    ///     public SimpleMessageUserControl()
    ///     {
    ///         this.InitializeComponent();
    ///     }
    /// }
    /// 
    /// // Uso:
    /// var accepted = await _dialogService.ShowDialogAsync&lt;SimpleMessageUserControl&gt;(
    ///     title: "Bienvenido",
    ///     primaryButtonText: "Aceptar",
    ///     closeButtonText: "Cerrar"
    /// );
    /// // accepted será true si el usuario presionó "Aceptar"
    /// 
    /// 
    /// // ============================================================================
    /// // EJEMPLO 2: Diálogo CON parámetros pero SIN resultado específico
    /// // ============================================================================
    /// // UserControl: UserDetailsUserControl.xaml.cs
    /// public sealed partial class UserDetailsUserControl : UserControl
    /// {
    ///     public string Username { get; set; }
    ///     public string Email { get; set; }
    ///     public bool IsAdmin { get; set; }
    ///     
    ///     public UserDetailsUserControl()
    ///     {
    ///         this.InitializeComponent();
    ///     }
    /// }
    /// 
    /// // Uso:
    /// var saved = await _dialogService.ShowDialogAsync&lt;UserDetailsUserControl&gt;(
    ///     configureControl: control =&gt; 
    ///     {
    ///         control.Username = "john.doe";
    ///         control.Email = "john@example.com";
    ///         control.IsAdmin = true;
    ///     },
    ///     title: "Detalles del Usuario",
    ///     primaryButtonText: "Guardar",
    ///     closeButtonText: "Cancelar"
    /// );
    /// // saved será true si el usuario presionó "Guardar"
    /// 
    /// 
    /// // ============================================================================
    /// // EJEMPLO 3: Diálogo SIN parámetros pero CON resultado de tipo string
    /// // ============================================================================
    /// // UserControl: InputTextUserControl.xaml.cs
    /// public sealed partial class InputTextUserControl : UserControl
    /// {
    ///     public string EnteredText { get; set; }
    ///     
    ///     public InputTextUserControl()
    ///     {
    ///         this.InitializeComponent();
    ///         EnteredText = string.Empty;
    ///     }
    ///     
    ///     private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    ///     {
    ///         EnteredText = ((TextBox)sender).Text;
    ///     }
    /// }
    /// 
    /// // Uso:
    /// var text = await _dialogService.ShowDialogAsync&lt;InputTextUserControl, string&gt;(
    ///     getResult: control =&gt; control.EnteredText,
    ///     title: "Ingrese un valor",
    ///     primaryButtonText: "Aceptar",
    ///     closeButtonText: "Cancelar"
    /// );
    /// if (!string.IsNullOrEmpty(text))
    /// {
    ///     Console.WriteLine($"Usuario ingresó: {text}");
    /// }
    /// 
    /// 
    /// // ============================================================================
    /// // EJEMPLO 4: Diálogo CON parámetros Y CON resultado de tipo bool
    /// // ============================================================================
    /// // UserControl: ConfirmActionUserControl.xaml.cs
    /// public sealed partial class ConfirmActionUserControl : UserControl
    /// {
    ///     public string Message { get; set; }
    ///     public string ActionName { get; set; }
    ///     public bool IsConfirmed { get; private set; }
    ///     
    ///     public ConfirmActionUserControl()
    ///     {
    ///         this.InitializeComponent();
    ///     }
    ///     
    ///     private void CheckBox_Checked(object sender, RoutedEventArgs e)
    ///     {
    ///         IsConfirmed = true;
    ///     }
    /// }
    /// 
    /// // Uso:
    /// var confirmed = await _dialogService.ShowDialogAsync&lt;ConfirmActionUserControl, bool&gt;(
    ///     configureControl: control =&gt; 
    ///     {
    ///         control.Message = "¿Está seguro de eliminar este registro?";
    ///         control.ActionName = "Eliminar";
    ///     },
    ///     getResult: control =&gt; control.IsConfirmed,
    ///     title: "Confirmar Acción",
    ///     primaryButtonText: "Sí, eliminar",
    ///     closeButtonText: "No, cancelar"
    /// );
    /// if (confirmed)
    /// {
    ///     // Proceder con la eliminación
    /// }
    /// 
    /// 
    /// // ============================================================================
    /// // EJEMPLO 5: Diálogo CON parámetros Y CON resultado de tipo List&lt;int&gt;
    /// // ============================================================================
    /// // UserControl: MultiSelectUserControl.xaml.cs
    /// public sealed partial class MultiSelectUserControl : UserControl
    /// {
    ///     public List&lt;string&gt; AvailableOptions { get; set; }
    ///     public List&lt;int&gt; SelectedIndices { get; private set; }
    ///     
    ///     public MultiSelectUserControl()
    ///     {
    ///         this.InitializeComponent();
    ///         SelectedIndices = new List&lt;int&gt;();
    ///     }
    ///     
    ///     private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    ///     {
    ///         SelectedIndices = ((ListView)sender).SelectedItems
    ///             .Cast&lt;string&gt;()
    ///             .Select(item =&gt; AvailableOptions.IndexOf(item))
    ///             .ToList();
    ///     }
    /// }
    /// 
    /// // Uso:
    /// var selectedIndices = await _dialogService.ShowDialogAsync&lt;MultiSelectUserControl, List&lt;int&gt;&gt;(
    ///     configureControl: control =&gt; 
    ///     {
    ///         control.AvailableOptions = new List&lt;string&gt; { "Opción 1", "Opción 2", "Opción 3" };
    ///     },
    ///     getResult: control =&gt; control.SelectedIndices,
    ///     title: "Seleccionar Opciones",
    ///     primaryButtonText: "Confirmar",
    ///     closeButtonText: "Cancelar"
    /// );
    /// if (selectedIndices != null &amp;&amp; selectedIndices.Count &gt; 0)
    /// {
    ///     foreach (var index in selectedIndices)
    ///     {
    ///         Console.WriteLine($"Seleccionado índice: {index}");
    ///     }
    /// }
    /// 
    /// 
    /// // ============================================================================
    /// // EJEMPLO 6: Diálogo CON parámetros Y CON resultado de objeto personalizado
    /// // ============================================================================
    /// // Modelo de resultado personalizado
    /// public class FormResult
    /// {
    ///     public string Name { get; set; }
    ///     public int Age { get; set; }
    ///     public string Email { get; set; }
    /// }
    /// 
    /// // UserControl: FormUserControl.xaml.cs
    /// public sealed partial class FormUserControl : UserControl
    /// {
    ///     public string InitialName { get; set; }
    ///     
    ///     private string _name;
    ///     private int _age;
    ///     private string _email;
    ///     
    ///     public FormUserControl()
    ///     {
    ///         this.InitializeComponent();
    ///     }
    ///     
    ///     public FormResult GetFormData()
    ///     {
    ///         return new FormResult
    ///         {
    ///             Name = _name,
    ///             Age = _age,
    ///             Email = _email
    ///         };
    ///     }
    /// }
    /// 
    /// // Uso:
    /// var formData = await _dialogService.ShowDialogAsync&lt;FormUserControl, FormResult&gt;(
    ///     configureControl: control =&gt; 
    ///     {
    ///         control.InitialName = "John Doe";
    ///     },
    ///     getResult: control =&gt; control.GetFormData(),
    ///     title: "Formulario de Usuario",
    ///     primaryButtonText: "Enviar",
    ///     closeButtonText: "Cancelar"
    /// );
    /// if (formData != null)
    /// {
    ///     Console.WriteLine($"Nombre: {formData.Name}, Edad: {formData.Age}, Email: {formData.Email}");
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public class DialogService : IDialogService
    {
        /// <summary>
        /// Muestra un diálogo con un UserControl sin parámetros y sin resultado específico.
        /// </summary>
        public async Task<bool> ShowDialogAsync<TUserControl>(
            string? title = null,
            string? primaryButtonText = null,
            string? secondaryButtonText = null,
            string? closeButtonText = null
        ) where TUserControl : UserControl, new()
        {
            var userControl = new TUserControl();
            
            // Si no hay botones configurados, usar popup con light dismiss
            if (string.IsNullOrWhiteSpace(primaryButtonText) && 
                string.IsNullOrWhiteSpace(secondaryButtonText) && 
                string.IsNullOrWhiteSpace(closeButtonText))
            {
                return await ShowLightDismissDialogAsync(userControl, title);
            }
            
            var dialog = CreateContentDialog(userControl, title, primaryButtonText, secondaryButtonText, closeButtonText);
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        /// <summary>
        /// Muestra un diálogo con un UserControl que recibe parámetros y no devuelve resultado específico.
        /// </summary>
        public async Task<bool> ShowDialogAsync<TUserControl>(
            Action<TUserControl> configureControl,
            string? title = null,
            string? primaryButtonText = null,
            string? secondaryButtonText = null,
            string? closeButtonText = null
        ) where TUserControl : UserControl, new()
        {
            if (configureControl == null)
                throw new ArgumentNullException(nameof(configureControl));

            var userControl = new TUserControl();
            configureControl(userControl);
            
            // Si no hay botones configurados, usar popup con light dismiss
            if (string.IsNullOrWhiteSpace(primaryButtonText) && 
                string.IsNullOrWhiteSpace(secondaryButtonText) && 
                string.IsNullOrWhiteSpace(closeButtonText))
            {
                return await ShowLightDismissDialogAsync(userControl, title);
            }
            
            var dialog = CreateContentDialog(userControl, title, primaryButtonText, secondaryButtonText, closeButtonText);
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        /// <summary>
        /// Muestra un diálogo con un UserControl sin parámetros y obtiene un resultado genérico.
        /// </summary>
        public async Task<TResult?> ShowDialogAsync<TUserControl, TResult>(
            Func<TUserControl, TResult> getResult,
            string? title = null,
            string? primaryButtonText = null,
            string? secondaryButtonText = null,
            string? closeButtonText = null
        ) where TUserControl : UserControl, new()
        {
            if (getResult == null)
                throw new ArgumentNullException(nameof(getResult));

            var userControl = new TUserControl();
            
            // Si no hay botones configurados, usar popup con light dismiss
            if (string.IsNullOrWhiteSpace(primaryButtonText) && 
                string.IsNullOrWhiteSpace(secondaryButtonText) && 
                string.IsNullOrWhiteSpace(closeButtonText))
            {
                var dismissed = await ShowLightDismissDialogAsync(userControl, title);
                // Si el popup fue cerrado por light dismiss, no retornar resultado
                return default;
            }
            
            var dialog = CreateContentDialog(userControl, title, primaryButtonText, secondaryButtonText, closeButtonText);
            var dialogResult = await dialog.ShowAsync();
            
            if (dialogResult == ContentDialogResult.Primary)
            {
                return getResult(userControl);
            }
            
            return default;
        }

        /// <summary>
        /// Muestra un diálogo con un UserControl que recibe parámetros y devuelve un resultado genérico.
        /// </summary>
        public async Task<TResult?> ShowDialogAsync<TUserControl, TResult>(
            Action<TUserControl> configureControl,
            Func<TUserControl, TResult> getResult,
            string? title = null,
            string? primaryButtonText = null,
            string? secondaryButtonText = null,
            string? closeButtonText = null
        ) where TUserControl : UserControl, new()
        {
            if (configureControl == null)
                throw new ArgumentNullException(nameof(configureControl));
            if (getResult == null)
                throw new ArgumentNullException(nameof(getResult));

            var userControl = new TUserControl();
            configureControl(userControl);
            
            // Si no hay botones configurados, usar popup con light dismiss
            if (string.IsNullOrWhiteSpace(primaryButtonText) && 
                string.IsNullOrWhiteSpace(secondaryButtonText) && 
                string.IsNullOrWhiteSpace(closeButtonText))
            {
                var dismissed = await ShowLightDismissDialogAsync(userControl, title);
                // Si el popup fue cerrado por light dismiss, no retornar resultado
                return default;
            }
            
            var dialog = CreateContentDialog(userControl, title, primaryButtonText, secondaryButtonText, closeButtonText);
            var dialogResult = await dialog.ShowAsync();
            
            if (dialogResult == ContentDialogResult.Primary)
            {
                return getResult(userControl);
            }
            
            return default;
        }

        /// <summary>
        /// Crea un ContentDialog configurado con el UserControl y los textos de botones especificados.
        /// </summary>
        /// <param name="content">El UserControl que se mostrará como contenido del diálogo.</param>
        /// <param name="title">Título del diálogo.</param>
        /// <param name="primaryButtonText">Texto del botón primario.</param>
        /// <param name="secondaryButtonText">Texto del botón secundario.</param>
        /// <param name="closeButtonText">Texto del botón de cerrar.</param>
        /// <returns>ContentDialog configurado.</returns>
        private ContentDialog CreateContentDialog(
            UserControl content,
            string? title,
            string? primaryButtonText,
            string? secondaryButtonText,
            string? closeButtonText)
        {
            var dialog = new ContentDialog
            {
                Content = content,
                XamlRoot = GetXamlRoot()
            };

            if (!string.IsNullOrWhiteSpace(title))
                dialog.Title = title;

            if (!string.IsNullOrWhiteSpace(primaryButtonText))
                dialog.PrimaryButtonText = primaryButtonText;

            if (!string.IsNullOrWhiteSpace(secondaryButtonText))
                dialog.SecondaryButtonText = secondaryButtonText;

            if (!string.IsNullOrWhiteSpace(closeButtonText))
                dialog.CloseButtonText = closeButtonText;

            return dialog;
        }

        /// <summary>
        /// Muestra un diálogo con light dismiss (se cierra al hacer clic fuera) cuando no hay botones configurados.
        /// Utiliza un Popup con LightDismissOverlayMode para permitir el cierre al hacer clic fuera.
        /// </summary>
        /// <param name="content">El UserControl que se mostrará como contenido.</param>
        /// <param name="title">Título del diálogo (opcional).</param>
        /// <returns>Siempre retorna false ya que no hay botón primario en este modo.</returns>
        private async Task<bool> ShowLightDismissDialogAsync(UserControl content, string? title)
        {
            var tcs = new TaskCompletionSource<bool>();
            
            // Crear un Border para contener el contenido con estilo de diálogo
            var border = new Border
            {
                Background = new SolidColorBrush(Microsoft.UI.Colors.White),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(24),
                MinWidth = 320,
                MaxWidth = 548,
                MaxHeight = 756
            };

            // Si hay título, crear un StackPanel con el título y el contenido
            if (!string.IsNullOrWhiteSpace(title))
            {
                var stackPanel = new StackPanel();
                
                var titleBlock = new TextBlock
                {
                    Text = title,
                    FontSize = 20,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 16)
                };
                
                stackPanel.Children.Add(titleBlock);
                stackPanel.Children.Add(content);
                border.Child = stackPanel;
            }
            else
            {
                border.Child = content;
            }

            // Crear el Popup con LightDismissOverlayMode
            var popup = new Popup
            {
                Child = border,
                IsLightDismissEnabled = true,
                LightDismissOverlayMode = LightDismissOverlayMode.On,
                XamlRoot = GetXamlRoot()
            };

            // Manejar el evento de cierre
            popup.Closed += (s, e) =>
            {
                tcs.TrySetResult(false);
            };

            // Centrar el popup en la pantalla
            var xamlRoot = GetXamlRoot();
            if (xamlRoot != null)
            {
                border.Loaded += (s, e) =>
                {
                    var windowWidth = xamlRoot.Size.Width;
                    var windowHeight = xamlRoot.Size.Height;
                    
                    popup.HorizontalOffset = (windowWidth - border.ActualWidth) / 2;
                    popup.VerticalOffset = (windowHeight - border.ActualHeight) / 2;
                };
            }

            // Mostrar el popup
            popup.IsOpen = true;

            // Esperar a que se cierre
            return await tcs.Task;
        }

        /// <summary>
        /// Obtiene el XamlRoot necesario para mostrar el ContentDialog.
        /// En WinUI 3, los diálogos requieren un XamlRoot para ser mostrados.
        /// </summary>
        /// <returns>XamlRoot de la ventana activa actual.</returns>
        /// <exception cref="InvalidOperationException">Si no se puede obtener el XamlRoot.</exception>
        private Microsoft.UI.Xaml.XamlRoot GetXamlRoot()
        {
            // En WinUI 3, Window.Current no está disponible, por lo que accedemos a la ventana
            // principal a través de App.MainWindow que fue establecida en App.OnLaunched
            if (App.MainWindow?.Content is Microsoft.UI.Xaml.FrameworkElement rootElement)
            {
                return rootElement.XamlRoot;
            }

            throw new InvalidOperationException(
                "No se pudo obtener el XamlRoot. Asegúrese de que existe una ventana activa con contenido.");
        }
    }
}
