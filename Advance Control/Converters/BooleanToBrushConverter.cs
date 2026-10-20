using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Advance_Control.Converters
{
    /// <summary>
    /// Convierte un bool en uno de dos Brush. A diferencia de BooleanToSystemColorBrushConverter
    /// (colores fijos), aquí los brushes se asignan desde XAML con {ThemeResource ...}, así el
    /// fondo respeta el tema claro/oscuro y el texto por defecto sigue siendo legible.
    /// TrueBrush/FalseBrush son DependencyProperty: {ThemeResource} solo se puede asignar a
    /// propiedades de dependencia (en una propiedad CLR compila, pero truena al cargar el XAML).
    /// </summary>
    public class BooleanToBrushConverter : DependencyObject, IValueConverter
    {
        public static readonly DependencyProperty TrueBrushProperty =
            DependencyProperty.Register(nameof(TrueBrush), typeof(Brush), typeof(BooleanToBrushConverter), new PropertyMetadata(null));

        public static readonly DependencyProperty FalseBrushProperty =
            DependencyProperty.Register(nameof(FalseBrush), typeof(Brush), typeof(BooleanToBrushConverter), new PropertyMetadata(null));

        public Brush? TrueBrush
        {
            get => (Brush?)GetValue(TrueBrushProperty);
            set => SetValue(TrueBrushProperty, value);
        }

        public Brush? FalseBrush
        {
            get => (Brush?)GetValue(FalseBrushProperty);
            set => SetValue(FalseBrushProperty, value);
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not bool boolean)
                return DependencyProperty.UnsetValue;

            return (boolean ? TrueBrush : FalseBrush) ?? (object)DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotSupportedException();
    }
}
