using System;
using Microsoft.UI.Xaml.Data;
using Advance_Control.Services.AccessControl;

namespace Advance_Control.Converters
{
    /// <summary>
    /// Convierte un nivel de acceso requerido en bool (IsEnabled).
    /// Usa AccessControlService.Current para verificar si el usuario activo puede acceder.
    /// Uso en XAML: IsEnabled="{Binding Converter={StaticResource AccessLevelConverter}, ConverterParameter=1}"
    /// </summary>
    public class AccessLevelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter is string s && int.TryParse(s, out var requiredLevel))
                return AccessControlService.Current.CanAccess(requiredLevel);
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
