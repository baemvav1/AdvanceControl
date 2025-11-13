using Advance_Control.Converters;
using Microsoft.UI.Xaml.Media;
using Xunit;

namespace Advance_Control.Tests.Converters
{
    /// <summary>
    /// Pruebas unitarias para el convertidor de prioridad a color de fondo
    /// </summary>
    public class PriorityToBackgroundConverterTests
    {
        private readonly PriorityToBackgroundConverter _converter;

        public PriorityToBackgroundConverterTests()
        {
            _converter = new PriorityToBackgroundConverter();
        }

        [Theory]
        [InlineData(1)] // lightcoral
        [InlineData(2)] // orange
        [InlineData(3)] // goldenrod
        [InlineData(4)] // green
        [InlineData(5)] // greenyellow
        public void Convert_WithValidPriority_ReturnsSolidColorBrush(int priority)
        {
            // Act
            var result = _converter.Convert(priority, typeof(SolidColorBrush), null, "en-US");

            // Assert
            Assert.NotNull(result);
            Assert.IsType<SolidColorBrush>(result);
        }

        [Fact]
        public void Convert_WithPriority1_ReturnsLightCoral()
        {
            // Act
            var result = _converter.Convert(1, typeof(SolidColorBrush), null, "en-US") as SolidColorBrush;

            // Assert
            Assert.NotNull(result);
            var color = result.Color;
            // lightcoral RGB(240, 128, 128)
            Assert.Equal(255, color.A);
            Assert.Equal(240, color.R);
            Assert.Equal(128, color.G);
            Assert.Equal(128, color.B);
        }

        [Fact]
        public void Convert_WithPriority2_ReturnsOrange()
        {
            // Act
            var result = _converter.Convert(2, typeof(SolidColorBrush), null, "en-US") as SolidColorBrush;

            // Assert
            Assert.NotNull(result);
            var color = result.Color;
            // orange RGB(255, 165, 0)
            Assert.Equal(255, color.A);
            Assert.Equal(255, color.R);
            Assert.Equal(165, color.G);
            Assert.Equal(0, color.B);
        }

        [Fact]
        public void Convert_WithPriority3_ReturnsGoldenrod()
        {
            // Act
            var result = _converter.Convert(3, typeof(SolidColorBrush), null, "en-US") as SolidColorBrush;

            // Assert
            Assert.NotNull(result);
            var color = result.Color;
            // goldenrod RGB(218, 165, 32)
            Assert.Equal(255, color.A);
            Assert.Equal(218, color.R);
            Assert.Equal(165, color.G);
            Assert.Equal(32, color.B);
        }

        [Fact]
        public void Convert_WithPriority4_ReturnsGreen()
        {
            // Act
            var result = _converter.Convert(4, typeof(SolidColorBrush), null, "en-US") as SolidColorBrush;

            // Assert
            Assert.NotNull(result);
            var color = result.Color;
            // green RGB(0, 128, 0)
            Assert.Equal(255, color.A);
            Assert.Equal(0, color.R);
            Assert.Equal(128, color.G);
            Assert.Equal(0, color.B);
        }

        [Fact]
        public void Convert_WithPriority5_ReturnsGreenYellow()
        {
            // Act
            var result = _converter.Convert(5, typeof(SolidColorBrush), null, "en-US") as SolidColorBrush;

            // Assert
            Assert.NotNull(result);
            var color = result.Color;
            // greenyellow RGB(173, 255, 47)
            Assert.Equal(255, color.A);
            Assert.Equal(173, color.R);
            Assert.Equal(255, color.G);
            Assert.Equal(47, color.B);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        [InlineData(-1)]
        [InlineData(100)]
        public void Convert_WithInvalidPriority_ReturnsTransparentBrush(int priority)
        {
            // Act
            var result = _converter.Convert(priority, typeof(SolidColorBrush), null, "en-US") as SolidColorBrush;

            // Assert
            Assert.NotNull(result);
            var color = result.Color;
            // transparent RGBA(0, 0, 0, 0)
            Assert.Equal(0, color.A);
        }

        [Fact]
        public void Convert_WithNullValue_ReturnsTransparentBrush()
        {
            // Act
            var result = _converter.Convert(null, typeof(SolidColorBrush), null, "en-US") as SolidColorBrush;

            // Assert
            Assert.NotNull(result);
            var color = result.Color;
            Assert.Equal(0, color.A);
        }

        [Fact]
        public void Convert_WithStringValue_ReturnsTransparentBrush()
        {
            // Act
            var result = _converter.Convert("not a number", typeof(SolidColorBrush), null, "en-US") as SolidColorBrush;

            // Assert
            Assert.NotNull(result);
            var color = result.Color;
            Assert.Equal(0, color.A);
        }

        [Fact]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            // Arrange
            var brush = new SolidColorBrush();

            // Act & Assert
            Assert.Throws<NotImplementedException>(() => 
                _converter.ConvertBack(brush, typeof(int), null, "en-US"));
        }
    }
}
