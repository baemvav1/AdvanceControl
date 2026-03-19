using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Advance_Control.Converters
{
    public class LevelToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var color = value is int level
                ? ResolveColor(level)
                : Color.FromArgb(255, 0, 0, 0);

            return new SolidColorBrush(color);
        }

        internal static Color ResolveColor(int level) => level switch
        {
            1 => Color.FromArgb(255, 250, 250, 250), // darkest gray
            2 => Color.FromArgb(255, 250, 250, 250), // dark gray
            3 => Color.FromArgb(255, 250, 250, 250), // medium gray
            4 => Color.FromArgb(255, 75, 75, 75),    // light gray
            5 => Color.FromArgb(255, 25, 25, 25),    // lightest gray
            _ => Color.FromArgb(255, 0, 0, 0)        // black for invalid values
        };

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
