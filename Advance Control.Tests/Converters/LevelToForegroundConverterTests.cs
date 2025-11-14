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
        [InlineData(1)] // lightest gray
        [InlineData(2)] // light gray
        [InlineData(3)] // medium gray
        [InlineData(4)] // dark gray
        [InlineData(5)] // darkest gray
        public void Convert_WithValidLevel_ReturnsSolidColorBrush(int level)
        {
            // Act
            var result = _converter.Convert(level, typeof(SolidColorBrush), null, "en-US");

            // Assert
            Assert.NotNull(result);
            Assert.IsType<SolidColorBrush>(result);
        }

        [Fact]
        public void Convert_WithLevel5_ReturnsDarkestGray()
        {
            // Act
            var result = _converter.Convert(5, typeof(SolidColorBrush), null, "en-US") as SolidColorBrush;

            // Assert
            Assert.NotNull(result);
            var color = result.Color;
            // darkest gray ARGB(255, 25, 25, 25)
            Assert.Equal(255, color.A);
            Assert.Equal(25, color.R);
            Assert.Equal(25, color.G);
            Assert.Equal(25, color.B);
        }

        [Fact]
        public void Convert_WithLevel4_ReturnsDarkGray()
        {
            // Act
            var result = _converter.Convert(4, typeof(SolidColorBrush), null, "en-US") as SolidColorBrush;

            // Assert
            Assert.NotNull(result);
            var color = result.Color;
            // dark gray ARGB(255, 75, 75, 75)
            Assert.Equal(255, color.A);
            Assert.Equal(75, color.R);
            Assert.Equal(75, color.G);
            Assert.Equal(75, color.B);
        }

        [Fact]
        public void Convert_WithLevel3_ReturnsMediumGray()
        {
            // Act
            var result = _converter.Convert(3, typeof(SolidColorBrush), null, "en-US") as SolidColorBrush;

            // Assert
            Assert.NotNull(result);
            var color = result.Color;
            // medium gray ARGB(255, 125, 125, 125)
            Assert.Equal(255, color.A);
            Assert.Equal(125, color.R);
            Assert.Equal(125, color.G);
            Assert.Equal(125, color.B);
        }

        [Fact]
        public void Convert_WithLevel2_ReturnsLightGray()
        {
            // Act
            var result = _converter.Convert(2, typeof(SolidColorBrush), null, "en-US") as SolidColorBrush;

            // Assert
            Assert.NotNull(result);
            var color = result.Color;
            // light gray ARGB(255, 200, 200, 200)
            Assert.Equal(255, color.A);
            Assert.Equal(200, color.R);
            Assert.Equal(200, color.G);
            Assert.Equal(200, color.B);
        }

        [Fact]
        public void Convert_WithLevel1_ReturnsLightestGray()
        {
            // Act
            var result = _converter.Convert(1, typeof(SolidColorBrush), null, "en-US") as SolidColorBrush;

            // Assert
            Assert.NotNull(result);
            var color = result.Color;
            // lightest gray ARGB(255, 225, 225, 225)
            Assert.Equal(255, color.A);
            Assert.Equal(225, color.R);
            Assert.Equal(225, color.G);
            Assert.Equal(225, color.B);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        [InlineData(-1)]
        [InlineData(100)]
        public void Convert_WithInvalidLevel_ReturnsBlackBrush(int level)
        {
            // Act
            var result = _converter.Convert(level, typeof(SolidColorBrush), null, "en-US") as SolidColorBrush;

            // Assert
            Assert.NotNull(result);
            var color = result.Color;
            // black ARGB(255, 0, 0, 0)
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
