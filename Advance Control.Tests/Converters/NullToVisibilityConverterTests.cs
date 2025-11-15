using System;
using Advance_Control.Converters;
using Microsoft.UI.Xaml;
using Xunit;

namespace Advance_Control.Tests.Converters
{
    /// <summary>
    /// Pruebas unitarias para NullToVisibilityConverter
    /// </summary>
    public class NullToVisibilityConverterTests
    {
        private readonly NullToVisibilityConverter _converter;

        public NullToVisibilityConverterTests()
        {
            _converter = new NullToVisibilityConverter();
        }

        [Fact]
        public void Convert_ConValorNull_RetornaCollapsed()
        {
            // Arrange
            object? value = null;

            // Act
            var result = _converter.Convert(value, typeof(Visibility), null, "en-US");

            // Assert
            Assert.Equal(Visibility.Collapsed, result);
        }

        [Fact]
        public void Convert_ConValorNoNull_RetornaVisible()
        {
            // Arrange
            var value = "Test string";

            // Act
            var result = _converter.Convert(value, typeof(Visibility), null, "en-US");

            // Assert
            Assert.Equal(Visibility.Visible, result);
        }

        [Fact]
        public void Convert_ConStringVacio_RetornaCollapsed()
        {
            // Arrange
            var value = "";

            // Act
            var result = _converter.Convert(value, typeof(Visibility), null, "en-US");

            // Assert
            Assert.Equal(Visibility.Collapsed, result);
        }

        [Fact]
        public void Convert_ConStringWhitespace_RetornaCollapsed()
        {
            // Arrange
            var value = "   ";

            // Act
            var result = _converter.Convert(value, typeof(Visibility), null, "en-US");

            // Assert
            Assert.Equal(Visibility.Collapsed, result);
        }

        [Fact]
        public void Convert_ConStringConContenido_RetornaVisible()
        {
            // Arrange
            var value = "Contenido válido";

            // Act
            var result = _converter.Convert(value, typeof(Visibility), null, "en-US");

            // Assert
            Assert.Equal(Visibility.Visible, result);
        }

        [Fact]
        public void Convert_ConObjetoNoNull_RetornaVisible()
        {
            // Arrange
            var value = new object();

            // Act
            var result = _converter.Convert(value, typeof(Visibility), null, "en-US");

            // Assert
            Assert.Equal(Visibility.Visible, result);
        }

        [Fact]
        public void ConvertBack_LanzaNotImplementedException()
        {
            // Arrange & Act & Assert
            Assert.Throws<NotImplementedException>(() =>
                _converter.ConvertBack(Visibility.Visible, typeof(object), null, "en-US"));
        }
    }
}
