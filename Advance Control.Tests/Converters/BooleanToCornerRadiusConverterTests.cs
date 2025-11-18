using System;
using Advance_Control.Converters;
using Microsoft.UI.Xaml;
using Xunit;

namespace Advance_Control.Tests.Converters
{
    /// <summary>
    /// Pruebas unitarias para BooleanToCornerRadiusConverter
    /// </summary>
    public class BooleanToCornerRadiusConverterTests
    {
        private readonly BooleanToCornerRadiusConverter _converter;

        public BooleanToCornerRadiusConverterTests()
        {
            _converter = new BooleanToCornerRadiusConverter();
        }

        [Fact]
        public void Convert_TrueConValorUniforme_RetornaCornerRadiusUniforme()
        {
            // Arrange
            bool value = true;
            string parameter = "8|0";

            // Act
            var result = _converter.Convert(value, typeof(CornerRadius), parameter, "en-US");

            // Assert
            Assert.IsType<CornerRadius>(result);
            var cornerRadius = (CornerRadius)result;
            Assert.Equal(8, cornerRadius.TopLeft);
            Assert.Equal(8, cornerRadius.TopRight);
            Assert.Equal(8, cornerRadius.BottomRight);
            Assert.Equal(8, cornerRadius.BottomLeft);
        }

        [Fact]
        public void Convert_FalseConValorUniforme_RetornaCornerRadiusUniforme()
        {
            // Arrange
            bool value = false;
            string parameter = "8|0";

            // Act
            var result = _converter.Convert(value, typeof(CornerRadius), parameter, "en-US");

            // Assert
            Assert.IsType<CornerRadius>(result);
            var cornerRadius = (CornerRadius)result;
            Assert.Equal(0, cornerRadius.TopLeft);
            Assert.Equal(0, cornerRadius.TopRight);
            Assert.Equal(0, cornerRadius.BottomRight);
            Assert.Equal(0, cornerRadius.BottomLeft);
        }

        [Fact]
        public void Convert_TrueConCuatroValores_RetornaCornerRadiusEspecifico()
        {
            // Arrange
            bool value = true;
            string parameter = "8,8,0,0|0";

            // Act
            var result = _converter.Convert(value, typeof(CornerRadius), parameter, "en-US");

            // Assert
            Assert.IsType<CornerRadius>(result);
            var cornerRadius = (CornerRadius)result;
            Assert.Equal(8, cornerRadius.TopLeft);
            Assert.Equal(8, cornerRadius.TopRight);
            Assert.Equal(0, cornerRadius.BottomRight);
            Assert.Equal(0, cornerRadius.BottomLeft);
        }

        [Fact]
        public void Convert_FalseConCuatroValores_RetornaCornerRadiusUniforme()
        {
            // Arrange
            bool value = false;
            string parameter = "8,8,0,0|0";

            // Act
            var result = _converter.Convert(value, typeof(CornerRadius), parameter, "en-US");

            // Assert
            Assert.IsType<CornerRadius>(result);
            var cornerRadius = (CornerRadius)result;
            Assert.Equal(0, cornerRadius.TopLeft);
            Assert.Equal(0, cornerRadius.TopRight);
            Assert.Equal(0, cornerRadius.BottomRight);
            Assert.Equal(0, cornerRadius.BottomLeft);
        }

        [Fact]
        public void Convert_TrueConValoresInferiores_RetornaCornerRadiusInferiores()
        {
            // Arrange
            bool value = true;
            string parameter = "0,0,8,8|8";

            // Act
            var result = _converter.Convert(value, typeof(CornerRadius), parameter, "en-US");

            // Assert
            Assert.IsType<CornerRadius>(result);
            var cornerRadius = (CornerRadius)result;
            Assert.Equal(0, cornerRadius.TopLeft);
            Assert.Equal(0, cornerRadius.TopRight);
            Assert.Equal(8, cornerRadius.BottomRight);
            Assert.Equal(8, cornerRadius.BottomLeft);
        }

        [Fact]
        public void Convert_FalseConValoresInferiores_RetornaCornerRadiusUniforme()
        {
            // Arrange
            bool value = false;
            string parameter = "0,0,8,8|8";

            // Act
            var result = _converter.Convert(value, typeof(CornerRadius), parameter, "en-US");

            // Assert
            Assert.IsType<CornerRadius>(result);
            var cornerRadius = (CornerRadius)result;
            Assert.Equal(8, cornerRadius.TopLeft);
            Assert.Equal(8, cornerRadius.TopRight);
            Assert.Equal(8, cornerRadius.BottomRight);
            Assert.Equal(8, cornerRadius.BottomLeft);
        }

        [Fact]
        public void Convert_SinParametro_RetornaCornerRadiusCero()
        {
            // Arrange
            bool value = true;

            // Act
            var result = _converter.Convert(value, typeof(CornerRadius), null, "en-US");

            // Assert
            Assert.IsType<CornerRadius>(result);
            var cornerRadius = (CornerRadius)result;
            Assert.Equal(0, cornerRadius.TopLeft);
            Assert.Equal(0, cornerRadius.TopRight);
            Assert.Equal(0, cornerRadius.BottomRight);
            Assert.Equal(0, cornerRadius.BottomLeft);
        }

        [Fact]
        public void Convert_ParametroInvalido_RetornaCornerRadiusCero()
        {
            // Arrange
            bool value = true;
            string parameter = "invalid";

            // Act
            var result = _converter.Convert(value, typeof(CornerRadius), parameter, "en-US");

            // Assert
            Assert.IsType<CornerRadius>(result);
            var cornerRadius = (CornerRadius)result;
            Assert.Equal(0, cornerRadius.TopLeft);
            Assert.Equal(0, cornerRadius.TopRight);
            Assert.Equal(0, cornerRadius.BottomRight);
            Assert.Equal(0, cornerRadius.BottomLeft);
        }

        [Fact]
        public void Convert_ValorNoBooleano_RetornaCornerRadiusCero()
        {
            // Arrange
            object value = "not a boolean";
            string parameter = "8|0";

            // Act
            var result = _converter.Convert(value, typeof(CornerRadius), parameter, "en-US");

            // Assert
            Assert.IsType<CornerRadius>(result);
            var cornerRadius = (CornerRadius)result;
            Assert.Equal(0, cornerRadius.TopLeft);
            Assert.Equal(0, cornerRadius.TopRight);
            Assert.Equal(0, cornerRadius.BottomRight);
            Assert.Equal(0, cornerRadius.BottomLeft);
        }

        [Fact]
        public void ConvertBack_LanzaNotImplementedException()
        {
            // Arrange & Act & Assert
            Assert.Throws<NotImplementedException>(() =>
                _converter.ConvertBack(new CornerRadius(8), typeof(bool), null, "en-US"));
        }
    }
}
