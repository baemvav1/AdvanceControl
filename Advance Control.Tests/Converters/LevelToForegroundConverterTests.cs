using Advance_Control.Converters;
using Microsoft.UI.Xaml.Media;
using Xunit;

namespace Advance_Control.Tests.Converters
{
    /// <summary>
    /// Pruebas unitarias para el convertidor de nivel a color de texto
    /// </summary>
    public class LevelToForegroundConverterTests
    {
        private readonly LevelToForegroundConverter _converter;

        public LevelToForegroundConverterTests()
        {
            _converter = new LevelToForegroundConverter();
        }

        [Theory]
        [InlineData(1)] // Contraste para lightcoral
        [InlineData(2)] // Contraste para orange
        [InlineData(3)] // Contraste para goldenrod
        [InlineData(4)] // Contraste para green
        [InlineData(5)] // Contraste para greenyellow
        public void Convert_WithValidPriority_ReturnsSolidColorBrush(int priority)
        {
            // Act
            var result = _converter.Convert(priority, typeof(SolidColorBrush), null, "en-US");

            // Assert
            Assert.NotNull(result);
            Assert.IsType<SolidColorBrush>(result);
        }

        [Fact]
        public void Convert_WithPriority1_ReturnsContrastColorForLightCoral()
        {
            // Act
            var result = _converter.Convert(1, typeof(SolidColorBrush), null, "en-US") as SolidColorBrush;

            // Assert
            Assert.NotNull(result);
            var color = result.Color;
            // Contraste para lightcoral RGB(15, 127, 127)
            Assert.Equal(255, color.A);
            Assert.Equal(15, color.R);
            Assert.Equal(127, color.G);
            Assert.Equal(127, color.B);
        }

        [Fact]
        public void Convert_WithPriority2_ReturnsContrastColorForOrange()
        {
            // Act
            var result = _converter.Convert(2, typeof(SolidColorBrush), null, "en-US") as SolidColorBrush;

            // Assert
            Assert.NotNull(result);
            var color = result.Color;
            // Contraste para orange RGB(0, 90, 255)
            Assert.Equal(255, color.A);
            Assert.Equal(0, color.R);
            Assert.Equal(90, color.G);
            Assert.Equal(255, color.B);
        }

        [Fact]
        public void Convert_WithPriority3_ReturnsContrastColorForGoldenrod()
        {
            // Act
            var result = _converter.Convert(3, typeof(SolidColorBrush), null, "en-US") as SolidColorBrush;

            // Assert
            Assert.NotNull(result);
            var color = result.Color;
            // Contraste para goldenrod RGB(37, 90, 223)
            Assert.Equal(255, color.A);
            Assert.Equal(37, color.R);
            Assert.Equal(90, color.G);
            Assert.Equal(223, color.B);
        }

        [Fact]
        public void Convert_WithPriority4_ReturnsContrastColorForGreen()
        {
            // Act
            var result = _converter.Convert(4, typeof(SolidColorBrush), null, "en-US") as SolidColorBrush;

            // Assert
            Assert.NotNull(result);
            var color = result.Color;
            // Contraste para green RGB(255, 127, 255)
            Assert.Equal(255, color.A);
            Assert.Equal(255, color.R);
            Assert.Equal(127, color.G);
            Assert.Equal(255, color.B);
        }

        [Fact]
        public void Convert_WithPriority5_ReturnsContrastColorForGreenYellow()
        {
            // Act
            var result = _converter.Convert(5, typeof(SolidColorBrush), null, "en-US") as SolidColorBrush;

            // Assert
            Assert.NotNull(result);
            var color = result.Color;
            // Contraste para greenyellow RGB(82, 0, 208)
            Assert.Equal(255, color.A);
            Assert.Equal(82, color.R);
            Assert.Equal(0, color.G);
            Assert.Equal(208, color.B);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        [InlineData(-1)]
        [InlineData(100)]
        public void Convert_WithInvalidPriority_ReturnsBlackBrush(int priority)
        {
            // Act
            var result = _converter.Convert(priority, typeof(SolidColorBrush), null, "en-US") as SolidColorBrush;

            // Assert
            Assert.NotNull(result);
            var color = result.Color;
            // Negro RGB(0, 0, 0)
            Assert.Equal(255, color.A);
            Assert.Equal(0, color.R);
            Assert.Equal(0, color.G);
            Assert.Equal(0, color.B);
        }

        [Fact]
        public void Convert_WithNullValue_ReturnsBlackBrush()
        {
            // Act
            var result = _converter.Convert(null, typeof(SolidColorBrush), null, "en-US") as SolidColorBrush;

            // Assert
            Assert.NotNull(result);
            var color = result.Color;
            Assert.Equal(255, color.A);
            Assert.Equal(0, color.R);
            Assert.Equal(0, color.G);
            Assert.Equal(0, color.B);
        }

        [Fact]
        public void Convert_WithStringValue_ReturnsBlackBrush()
        {
            // Act
            var result = _converter.Convert("not a number", typeof(SolidColorBrush), null, "en-US") as SolidColorBrush;

            // Assert
            Assert.NotNull(result);
            var color = result.Color;
            Assert.Equal(255, color.A);
            Assert.Equal(0, color.R);
            Assert.Equal(0, color.G);
            Assert.Equal(0, color.B);
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
