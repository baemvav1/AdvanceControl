using System;
using Microsoft.UI.Xaml.Data;

namespace Advance_Control.Converters
{
    /// <summary>
    /// Converter that formats nullable numbers (int?, double?) to strings with fallback for null values
    /// </summary>
    public class NullableNumberToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Handle null values
            if (value == null)
            {
                return parameter?.ToString() ?? "N/A";
            }

            // Handle nullable integers
            if (value is int intValue)
            {
                return intValue.ToString();
            }

            // Handle nullable doubles with formatting
            if (value is double doubleValue)
            {
                // Check if parameter specifies a format (e.g., "F2" for 2 decimal places)
                var format = parameter?.ToString();
                if (!string.IsNullOrEmpty(format))
                {
                    return doubleValue.ToString(format);
                }
                return doubleValue.ToString("F2"); // Default to 2 decimal places
            }

            // Fallback for other types
            return value.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException("ConvertBack is not supported for NullableNumberToStringConverter");
        }
    }
}
