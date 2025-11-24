using System;
using Advance_Control.Converters;
using Xunit;

namespace Advance_Control.Tests.Converters
{
    /// <summary>
    /// Pruebas unitarias para BooleanToArrowConverter
    /// </summary>
    public class BooleanToArrowConverterTests
    {
        private readonly BooleanToArrowConverter _converter;

        public BooleanToArrowConverterTests()
        {
            _converter = new BooleanToArrowConverter();
        }

        [Fact]
        public void Convert_WhenTrue_ReturnsRightArrow()
        {
            // Arrange - When panel is expanded (visible), show right arrow
            var value = true;

            // Act
            var result = _converter.Convert(value, typeof(string), null, "en-US");

            // Assert
            Assert.Equal("→", result);
        }

        [Fact]
        public void Convert_WhenFalse_ReturnsLeftArrow()
        {
            // Arrange - When panel is collapsed (hidden), show left arrow
            var value = false;

            // Act
            var result = _converter.Convert(value, typeof(string), null, "en-US");

            // Assert
            Assert.Equal("←", result);
        }

        [Fact]
        public void Convert_WhenNotBool_ReturnsLeftArrow()
        {
            // Arrange
            var value = "not a bool";

            // Act
            var result = _converter.Convert(value, typeof(string), null, "en-US");

            // Assert
            Assert.Equal("←", result);
        }

        [Fact]
        public void Convert_WhenNull_ReturnsLeftArrow()
        {
            // Arrange
            object? value = null;

            // Act
            var result = _converter.Convert(value, typeof(string), null, "en-US");

            // Assert
            Assert.Equal("←", result);
        }

        [Fact]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            // Arrange & Act & Assert
            Assert.Throws<NotImplementedException>(() =>
                _converter.ConvertBack("→", typeof(bool), null, "en-US"));
        }
    }
}
