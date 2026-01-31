using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Advance_Control.Converters
{
    /// <summary>
    /// Converter that shows a control only when TipoCargo is "Refaccion"
    /// </summary>
    public class RefaccionVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string tipoCargo)
            {
                return tipoCargo.Equals("Refaccion", StringComparison.OrdinalIgnoreCase)
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
