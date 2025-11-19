using System;
using Microsoft.UI.Xaml.Data;

namespace Advance_Control.Converters
{
    /// <summary>
    /// Converter that converts a boolean expand state to a text symbol.
    /// When true (expanded): shows "▼" (down arrow)
    /// When false (collapsed): shows "▶" (right arrow)
    /// </summary>
    public class BooleanToExpandTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "▼" : "▶";
            }
            return "▶";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
