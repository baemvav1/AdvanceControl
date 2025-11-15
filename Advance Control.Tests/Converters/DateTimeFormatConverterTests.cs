using System;
using Advance_Control.Converters;
using Xunit;

namespace Advance_Control.Tests.Converters
{
    /// <summary>
    /// Pruebas unitarias para DateTimeFormatConverter
    /// </summary>
    public class DateTimeFormatConverterTests
    {
        private readonly DateTimeFormatConverter _converter;

        public DateTimeFormatConverterTests()
        {
            _converter = new DateTimeFormatConverter();
        }

        [Fact]
        public void Convert_ConDateTime_RetornaFormatoEsperado()
        {
            // Arrange
            var date = new DateTime(2025, 11, 15, 14, 30, 0);

            // Act
            var result = _converter.Convert(date, typeof(string), null, "en-US");

            // Assert
            Assert.Equal("15/11/2025 14:30", result);
        }

        [Fact]
        public void Convert_ConValorNull_RetornaStringVacio()
        {
            // Arrange
            object? value = null;

            // Act
            var result = _converter.Convert(value, typeof(string), null, "en-US");

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Convert_ConDateTimeMedianoche_RetornaFormatoConCeros()
        {
            // Arrange
            var date = new DateTime(2025, 1, 1, 0, 0, 0);

            // Act
            var result = _converter.Convert(date, typeof(string), null, "en-US");

            // Assert
            Assert.Equal("01/01/2025 00:00", result);
        }

        [Fact]
        public void Convert_ConDateTimeFinDeDia_RetornaFormatoCompleto()
        {
            // Arrange
            var date = new DateTime(2025, 12, 31, 23, 59, 0);

            // Act
            var result = _converter.Convert(date, typeof(string), null, "en-US");

            // Assert
            Assert.Equal("31/12/2025 23:59", result);
        }

        [Fact]
        public void Convert_ConObjetoNoDateTime_UsaToString()
        {
            // Arrange
            var value = 12345;

            // Act
            var result = _converter.Convert(value, typeof(string), null, "en-US");

            // Assert
            Assert.Equal("12345", result);
        }

        [Fact]
        public void ConvertBack_LanzaNotImplementedException()
        {
            // Arrange & Act & Assert
            Assert.Throws<NotImplementedException>(() =>
                _converter.ConvertBack("15/11/2025 14:30", typeof(DateTime), null, "en-US"));
        }
    }
}
