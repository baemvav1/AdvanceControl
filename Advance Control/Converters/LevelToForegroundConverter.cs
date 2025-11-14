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
            if (value is int priority)
            {
                Color color = priority switch
                {
                    1 => ColorHelper.FromArgb(255, 15, 127, 127),   // Contraste para lightcoral (240, 128, 128)
                    2 => ColorHelper.FromArgb(255, 0, 90, 255),     // Contraste para orange (255, 165, 0)
                    3 => ColorHelper.FromArgb(255, 37, 90, 223),    // Contraste para goldenrod (218, 165, 32)
                    4 => ColorHelper.FromArgb(255, 255, 127, 255),  // Contraste para green (0, 128, 0)
                    5 => ColorHelper.FromArgb(255, 82, 0, 208),     // Contraste para greenyellow (173, 255, 47)
                    _ => ColorHelper.FromArgb(255, 0, 0, 0)         // Negro para valores inválidos
                };
                
                return new SolidColorBrush(color);
            }
            
            return new SolidColorBrush(ColorHelper.FromArgb(255, 0, 0, 0)); // Negro
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
