using Microsoft.UI.Xaml.Data;
using System;

namespace Advance_Control.Converters
{
    /// <summary>
    /// Convierte un valor booleano a su inverso.
    /// </summary>
    public class BoolNegationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            // Si no es booleano, retornar false para comportamiento predecible
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            // Consistente con Convert, retornar false para valores no-booleanos
            return false;
        }
    }
}
