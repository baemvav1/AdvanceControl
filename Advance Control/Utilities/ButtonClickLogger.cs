using System;
using Advance_Control.Services.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Advance_Control.Utilities
{
    /// <summary>
    /// Adjunta un handler global a un UIElement (normalmente una Page) que captura
    /// el evento Tapped de cualquier ButtonBase dentro de él mediante event-bubbling.
    /// Genera un log por cada clic, con el nombre del botón y la página de origen.
    /// </summary>
    public static class ButtonClickLogger
    {
        /// <summary>
        /// Adjunta el listener al elemento raíz indicado.
        /// Llama a este método después de InitializeComponent() en el constructor de la Page.
        /// </summary>
        public static void Attach(UIElement root, ILoggingService logger, string pageName)
        {
            if (root is null || logger is null) return;

            // UIElement.TappedEvent burbujea desde botones y llega al nivel de la Page.
            // handledEventsToo:true asegura que lo capturamos aunque el botón marque el evento handled.
            root.AddHandler(
                UIElement.TappedEvent,
                new TappedEventHandler((_, e) =>
                {
                    if (e.OriginalSource is DependencyObject source)
                    {
                        // Camina hacia arriba en el árbol visual para encontrar el ButtonBase
                        var btn = FindAncestorOrSelf<ButtonBase>(source);
                        if (btn != null)
                        {
                            var name = ResolveName(btn);
                            if (!string.IsNullOrWhiteSpace(name))
                                _ = logger.LogInformationAsync(
                                    $"Botón: {name}",
                                    source: pageName,
                                    method: "ButtonClick");
                        }
                    }
                }),
                handledEventsToo: true);
        }

        private static T? FindAncestorOrSelf<T>(DependencyObject obj) where T : DependencyObject
        {
            while (obj != null)
            {
                if (obj is T target) return target;
                obj = VisualTreeHelper.GetParent(obj);
            }
            return null;
        }

        private static string? ResolveName(ButtonBase btn)
        {
            // 1. x:Name del botón
            if (!string.IsNullOrWhiteSpace(btn.Name)) return btn.Name;
            // 2. Contenido de texto directo
            if (btn.Content is string s && !string.IsNullOrWhiteSpace(s)) return s.Trim();
            // 3. Tag como string
            if (btn.Tag is string t && !string.IsNullOrWhiteSpace(t)) return t.Trim();
            return null;
        }
    }
}
