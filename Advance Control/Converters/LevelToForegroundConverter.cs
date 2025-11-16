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
                    1 => ColorHelper.FromArgb(255, 250, 250, 250),     // darkest gray
                    2 => ColorHelper.FromArgb(255, 250, 250, 250),     // dark gray
                    3 => ColorHelper.FromArgb(255, 250, 250, 250),  // medium gray
                    4 => ColorHelper.FromArgb(255, 75, 75, 75),  // light gray
                    5 => ColorHelper.FromArgb(255, 25, 25, 25),  // lightest gray
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
