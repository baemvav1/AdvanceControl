using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Advance_Control.Services.PermisosUi
{
    public static class PermisoUiKeyBuilder
    {
        private const string UnknownModule = "UnknownModule";

        public static readonly HashSet<string> SupportedActionTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Button",
            "AppBarButton",
            "ToggleButton",
            "RepeatButton",
            "HyperlinkButton",
            "DropDownButton",
            "SplitButton",
            "ToggleSplitButton",
            "MenuFlyoutItem",
            "ToggleMenuFlyoutItem"
        };

        public static string BuildModuleKey(string? xamlClass)
        {
            return string.IsNullOrWhiteSpace(xamlClass) ? UnknownModule : xamlClass.Trim();
        }

        public static string BuildModuleKey(Type pageType)
        {
            return BuildModuleKey(pageType.FullName);
        }

        public static string BuildActionKey(string moduleKey, string controlType, string controlKey)
        {
            return $"{moduleKey}::{controlType}::{controlKey}";
        }

        public static string? ResolveRuntimeControlKey(FrameworkElement element)
        {
            if (!string.IsNullOrWhiteSpace(element.Name))
                return element.Name.Trim();

            if (element.Tag is string tag && !string.IsNullOrWhiteSpace(tag))
                return tag.Trim();

            if (element is ButtonBase buttonBase && buttonBase.Content is string buttonContent && !string.IsNullOrWhiteSpace(buttonContent))
                return buttonContent.Trim();

            if (element is DropDownButton dropDownButton && dropDownButton.Content is string dropDownContent && !string.IsNullOrWhiteSpace(dropDownContent))
                return dropDownContent.Trim();

            if (element is SplitButton splitButton && splitButton.Content is string splitContent && !string.IsNullOrWhiteSpace(splitContent))
                return splitContent.Trim();

            if (element is ToggleSplitButton toggleSplitButton && toggleSplitButton.Content is string toggleSplitContent && !string.IsNullOrWhiteSpace(toggleSplitContent))
                return toggleSplitContent.Trim();

            return null;
        }
    }
}
