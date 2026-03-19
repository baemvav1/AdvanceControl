using Advance_Control.Converters;
using Xunit;

namespace Advance_Control.Tests.Converters
{
    public class LevelToForegroundConverterTests
    {
        [Theory]
        [InlineData(1, 255, 250, 250, 250)]
        [InlineData(2, 255, 250, 250, 250)]
        [InlineData(3, 255, 250, 250, 250)]
        [InlineData(4, 255, 75, 75, 75)]
        [InlineData(5, 255, 25, 25, 25)]
        public void ResolveColor_WithValidLevel_ReturnsExpectedColor(int level, byte a, byte r, byte g, byte b)
        {
            var color = LevelToForegroundConverter.ResolveColor(level);

            Assert.Equal(a, color.A);
            Assert.Equal(r, color.R);
            Assert.Equal(g, color.G);
            Assert.Equal(b, color.B);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        [InlineData(-1)]
        [InlineData(100)]
        public void ResolveColor_WithInvalidLevel_ReturnsBlack(int level)
        {
            var color = LevelToForegroundConverter.ResolveColor(level);

            Assert.Equal(255, color.A);
            Assert.Equal(0, color.R);
            Assert.Equal(0, color.G);
            Assert.Equal(0, color.B);
        }

        [Fact]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            var converter = new LevelToForegroundConverter();

            Assert.Throws<NotImplementedException>(() =>
                converter.ConvertBack(null!, typeof(int), null!, "en-US"));
        }
    }
}
