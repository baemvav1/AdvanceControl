using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Windows.UI;

namespace Advance_Control.Converters
{
    public class LevelToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int level)
            {
                Color color = level switch
                {
                    5 => ColorHelper.FromArgb(255, 25, 25, 25),     // darkest gray
                    4 => ColorHelper.FromArgb(255, 75, 75, 75),     // dark gray
                    3 => ColorHelper.FromArgb(255, 125, 125, 125),  // medium gray
                    2 => ColorHelper.FromArgb(255, 200, 200, 200),  // light gray
                    1 => ColorHelper.FromArgb(255, 225, 225, 225),  // lightest gray
                    _ => ColorHelper.FromArgb(255, 0, 0, 0)         // black for invalid values
                };
                
                return new SolidColorBrush(color);
            }
            
            return new SolidColorBrush(ColorHelper.FromArgb(255, 0, 0, 0)); // black
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
