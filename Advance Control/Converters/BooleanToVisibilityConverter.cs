using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace AdvanceControl.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolean)
            {
                bool invert = parameter is string str && str.Equals("invert", StringComparison.OrdinalIgnoreCase);
                bool useHidden = parameter is string str2 && str2.Equals("UseHidden", StringComparison.OrdinalIgnoreCase);

                if (invert)
                {
                    boolean = !boolean;
                }

                return boolean ? (useHidden ? Visibility.Hidden : Visibility.Visible) : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return null;
        }
    }
}