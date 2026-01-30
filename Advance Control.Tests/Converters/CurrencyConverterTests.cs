using System;
using Advance_Control.Converters;
using Xunit;

namespace Advance_Control.Tests.Converters
{
    /// <summary>
    /// Pruebas unitarias para CurrencyConverter
    /// </summary>
    public class CurrencyConverterTests
    {
        private readonly CurrencyConverter _converter;

        public CurrencyConverterTests()
        {
            _converter = new CurrencyConverter();
        }

        [Fact]
        public void Convert_ConDouble_RetornaFormatoMoneda()
        {
            // Arrange
            double value = 1234.56;

            // Act
            var result = _converter.Convert(value, typeof(string), null, "es-MX");

            // Assert
            Assert.Contains("1,234.56", result.ToString());
            Assert.Contains("$", result.ToString());
        }

        [Fact]
        public void Convert_ConDecimal_RetornaFormatoMoneda()
        {
            // Arrange
            decimal value = 999.99m;

            // Act
            var result = _converter.Convert(value, typeof(string), null, "es-MX");

            // Assert
            Assert.Contains("999.99", result.ToString());
            Assert.Contains("$", result.ToString());
        }

        [Fact]
        public void Convert_ConInt_RetornaFormatoMoneda()
        {
            // Arrange
            int value = 1000;

            // Act
            var result = _converter.Convert(value, typeof(string), null, "es-MX");

            // Assert
            Assert.Contains("1,000.00", result.ToString());
            Assert.Contains("$", result.ToString());
        }

        [Fact]
        public void Convert_ConValorNull_RetornaCero()
        {
            // Arrange
            object? value = null;

            // Act
            var result = _converter.Convert(value, typeof(string), null, "es-MX");

            // Assert
            Assert.Equal("$0.00", result);
        }

        [Fact]
        public void Convert_ConCero_RetornaFormatoMonedaCero()
        {
            // Arrange
            double value = 0.0;

            // Act
            var result = _converter.Convert(value, typeof(string), null, "es-MX");

            // Assert
            Assert.Contains("0.00", result.ToString());
            Assert.Contains("$", result.ToString());
        }

        [Fact]
        public void Convert_ConNumeroNegativo_RetornaFormatoMonedaNegativo()
        {
            // Arrange
            double value = -500.50;

            // Act
            var result = _converter.Convert(value, typeof(string), null, "es-MX");

            // Assert
            Assert.Contains("500.50", result.ToString());
            Assert.Contains("$", result.ToString());
        }

        [Fact]
        public void ConvertBack_ConStringConSignoPeso_RetornaDouble()
        {
            // Arrange
            string value = "$1,234.56";

            // Act
            var result = _converter.ConvertBack(value, typeof(double), null, "es-MX");

            // Assert
            Assert.Equal(1234.56, result);
        }

        [Fact]
        public void ConvertBack_ConStringSinSignoPeso_RetornaDouble()
        {
            // Arrange
            string value = "999.99";

            // Act
            var result = _converter.ConvertBack(value, typeof(double), null, "es-MX");

            // Assert
            Assert.Equal(999.99, result);
        }

        [Fact]
        public void ConvertBack_ConStringInvalida_RetornaCero()
        {
            // Arrange
            string value = "invalid";

            // Act
            var result = _converter.ConvertBack(value, typeof(double), null, "es-MX");

            // Assert
            Assert.Equal(0.0, result);
        }

        [Fact]
        public void ConvertBack_ConValorNoString_RetornaCero()
        {
            // Arrange
            object value = 123;

            // Act
            var result = _converter.ConvertBack(value, typeof(double), null, "es-MX");

            // Assert
            Assert.Equal(0.0, result);
        }
    }
}
