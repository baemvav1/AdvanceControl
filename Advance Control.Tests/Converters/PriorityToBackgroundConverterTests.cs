using Advance_Control.Converters;
using Xunit;

namespace Advance_Control.Tests.Converters
{
    public class LevelToBackgroundConverterTests
    {
        [Theory]
        [InlineData(1, 255, 25, 25, 25)]
        [InlineData(2, 255, 75, 75, 75)]
        [InlineData(3, 255, 125, 125, 125)]
        [InlineData(4, 255, 175, 175, 175)]
        [InlineData(5, 255, 225, 225, 225)]
        public void ResolveColor_WithValidPriority_ReturnsExpectedColor(int priority, byte a, byte r, byte g, byte b)
        {
            var color = LevelToBackgroundConverter.ResolveColor(priority);

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
        public void ResolveColor_WithInvalidPriority_ReturnsTransparent(int priority)
        {
            var color = LevelToBackgroundConverter.ResolveColor(priority);

            Assert.Equal(0, color.A);
            Assert.Equal(0, color.R);
            Assert.Equal(0, color.G);
            Assert.Equal(0, color.B);
        }

        [Fact]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            var converter = new LevelToBackgroundConverter();

            Assert.Throws<NotImplementedException>(() =>
                converter.ConvertBack(null!, typeof(int), null!, "en-US"));
        }
    }
}
