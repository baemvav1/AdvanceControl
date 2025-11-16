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
                    5 => ColorHelper.FromArgb(255, 225, 225, 225), // lightcoral
                    4 => ColorHelper.FromArgb(255, 175, 175, 175),   // orange
                    3 => ColorHelper.FromArgb(255, 125, 125, 125),  // goldenrod
                    2 => ColorHelper.FromArgb(255, 75, 75, 75),     // green
                    1 => ColorHelper.FromArgb(255, 25, 25, 25),  // greenyellow
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
