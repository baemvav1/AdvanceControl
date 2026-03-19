using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Advance_Control.Converters
{
    public class LevelToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var color = value is int priority
                ? ResolveColor(priority)
                : Color.FromArgb(0, 0, 0, 0);

            return new SolidColorBrush(color);
        }

        internal static Color ResolveColor(int priority) => priority switch
        {
            5 => Color.FromArgb(255, 225, 225, 225), // lightcoral
            4 => Color.FromArgb(255, 175, 175, 175), // orange
            3 => Color.FromArgb(255, 125, 125, 125), // goldenrod
            2 => Color.FromArgb(255, 75, 75, 75),    // green
            1 => Color.FromArgb(255, 25, 25, 25),    // greenyellow
            _ => Color.FromArgb(0, 0, 0, 0)          // transparent for invalid values
        };

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
