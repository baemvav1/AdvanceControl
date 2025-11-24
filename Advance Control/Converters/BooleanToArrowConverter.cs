using System;
using Microsoft.UI.Xaml.Data;

namespace Advance_Control.Converters
{
    /// <summary>
    /// Converter that converts a boolean state to a horizontal arrow symbol.
    /// When true (expanded): shows "→" (right arrow - to collapse panel)
    /// When false (collapsed): shows "←" (left arrow - to expand panel)
    /// </summary>
    public class BooleanToArrowConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                // When panel is visible (expanded), show right arrow (to collapse)
                // When panel is hidden (collapsed), show left arrow (to expand)
                return boolValue ? "→" : "←";
            }
            return "←";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
