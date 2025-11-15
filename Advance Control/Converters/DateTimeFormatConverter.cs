using Microsoft.UI.Xaml.Data;
using System;

namespace Advance_Control.Converters
{
    /// <summary>
    /// Convierte un DateTime a un formato de cadena legible.
    /// </summary>
    public class DateTimeFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is DateTime dateTime)
            {
                // Formato: 15/11/2025 14:30
                return dateTime.ToString("dd/MM/yyyy HH:mm");
            }

            return value.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
