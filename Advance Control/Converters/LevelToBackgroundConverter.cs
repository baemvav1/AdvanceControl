using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Windows.UI;

namespace Advance_Control.Converters
{
    public class LevelToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int priority)
            {
                Color color = priority switch
                {
                    1 => ColorHelper.FromArgb(255, 240, 128, 128), // lightcoral
                    2 => ColorHelper.FromArgb(255, 255, 165, 0),   // orange
                    3 => ColorHelper.FromArgb(255, 218, 165, 32),  // goldenrod
                    4 => ColorHelper.FromArgb(255, 0, 128, 0),     // green
                    5 => ColorHelper.FromArgb(255, 173, 255, 47),  // greenyellow
                    _ => ColorHelper.FromArgb(0, 0, 0, 0)          // transparent for invalid values
                };
                
                return new SolidColorBrush(color);
            }
            
            return new SolidColorBrush(ColorHelper.FromArgb(0, 0, 0, 0)); // transparent
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
