using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Advance_Control.Converters
{
    /// <summary>
    /// Converter that controls visibility based on cargo type selection.
    /// Parameter should be "1" for Refaccion or "2" for Servicio.
    /// </summary>
    public class CargoTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int selectedType && parameter is string targetTypeString)
            {
                if (int.TryParse(targetTypeString, out int target))
                {
                    return selectedType == target ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
