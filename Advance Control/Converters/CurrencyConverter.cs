using System;
using Microsoft.UI.Xaml.Data;

namespace Advance_Control.Converters
{
    /// <summary>
    /// Converter that formats numbers as currency in Mexican Pesos
    /// </summary>
    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Handle null values
            if (value == null)
            {
                return "$0.00";
            }

            // Handle doubles
            if (value is double doubleValue)
            {
                return doubleValue.ToString("C2", new System.Globalization.CultureInfo("es-MX"));
            }

            // Handle decimal
            if (value is decimal decimalValue)
            {
                return decimalValue.ToString("C2", new System.Globalization.CultureInfo("es-MX"));
            }

            // Handle integers
            if (value is int intValue)
            {
                return intValue.ToString("C2", new System.Globalization.CultureInfo("es-MX"));
            }

            // Fallback
            return "$0.00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // Remove currency symbols and parse back to double
            if (value is string stringValue)
            {
                stringValue = stringValue.Replace("$", "").Replace(",", "").Trim();
                if (double.TryParse(stringValue, out double result))
                {
                    return result;
                }
            }

            return 0.0;
        }
    }
}
