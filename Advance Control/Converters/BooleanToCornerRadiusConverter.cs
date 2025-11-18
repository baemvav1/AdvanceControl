using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Advance_Control.Converters
{
    /// <summary>
    /// Converter that converts a boolean value to a CornerRadius based on parameter values.
    /// Parameter format: "TrueValue|FalseValue" where each value is a comma-separated list of 1 or 4 numbers.
    /// Examples: 
    ///   "8,8,0,0|0" - When true: top corners rounded (8,8,0,0), when false: no rounding (0)
    ///   "8|0" - When true: all corners 8, when false: all corners 0
    /// </summary>
    public class BooleanToCornerRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue && parameter is string paramStr)
            {
                var parts = paramStr.Split('|');
                if (parts.Length == 2)
                {
                    var targetValue = boolValue ? parts[0] : parts[1];
                    return ParseCornerRadius(targetValue);
                }
            }
            return new CornerRadius(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        private CornerRadius ParseCornerRadius(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new CornerRadius(0);

            var values = value.Split(',');
            
            if (values.Length == 1)
            {
                // Single value for all corners
                if (double.TryParse(values[0].Trim(), out double uniform))
                    return new CornerRadius(uniform);
            }
            else if (values.Length == 4)
            {
                // Four values: topLeft, topRight, bottomRight, bottomLeft
                if (double.TryParse(values[0].Trim(), out double topLeft) &&
                    double.TryParse(values[1].Trim(), out double topRight) &&
                    double.TryParse(values[2].Trim(), out double bottomRight) &&
                    double.TryParse(values[3].Trim(), out double bottomLeft))
                {
                    return new CornerRadius(topLeft, topRight, bottomRight, bottomLeft);
                }
            }

            return new CornerRadius(0);
        }
    }
}
