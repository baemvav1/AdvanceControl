using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Advance_Control.Converters
{
    /// <summary>
    /// Convierte un int? a Visibility.Visible si tiene un valor mayor a 0, de lo contrario Visibility.Collapsed.
    /// Usado para mostrar botones solo cuando hay una relación válida (IdRelacionCargo > 0).
    /// </summary>
    public class NullableIntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int? nullableInt)
            {
                return nullableInt.HasValue && nullableInt.Value > 0 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
