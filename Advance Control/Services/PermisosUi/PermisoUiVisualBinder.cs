using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Advance_Control.Services.PermisosUi
{
    public static class PermisoUiVisualBinder
    {
        public static void ApplyToPage(Page page, IPermisoUiRuntimeService runtime)
        {
            if (page == null || runtime == null || !runtime.IsInitialized)
                return;

            var moduleKey = runtime.BuildModuleKey(page.GetType());
            foreach (var element in EnumerateDescendants(page).OfType<FrameworkElement>())
            {
                var controlType = element.GetType().Name;
                if (!PermisoUiKeyBuilder.SupportedActionTypes.Contains(controlType))
                    continue;

                var controlKey = PermisoUiKeyBuilder.ResolveRuntimeControlKey(element);
                if (string.IsNullOrWhiteSpace(controlKey))
                    continue;

                var actionKey = runtime.BuildActionKey(moduleKey, controlType, controlKey);
                if (runtime.CanAccessAction(actionKey))
                    continue;

                if (element is Control control)
                {
                    control.IsEnabled = false;
                }
            }
        }

        private static System.Collections.Generic.IEnumerable<DependencyObject> EnumerateDescendants(DependencyObject root)
        {
            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                yield return child;

                foreach (var descendant in EnumerateDescendants(child))
                    yield return descendant;
            }
        }
    }
}
