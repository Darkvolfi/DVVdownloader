using System.Globalization;
using System.Windows;
using System.Windows.Data;
using DVVdownloader.Converters;
using Xunit;

namespace DVVdownloader.Tests
{
    public class ConverterTests
    {
        [Fact]
        public void TabToBooleanConverter_ConvertAndConvertBack_NeverThrows()
        {
            var converter = new TabToBooleanConverter();
            var culture = CultureInfo.InvariantCulture;

            // SelectedTab = 0, Parameter = "0" -> true
            Assert.True((bool)converter.Convert(0, typeof(bool), "0", culture));
            // SelectedTab = 1, Parameter = "0" -> false
            Assert.False((bool)converter.Convert(1, typeof(bool), "0", culture));

            // ConvertBack when true -> returns target tab int
            Assert.Equal(2, converter.ConvertBack(true, typeof(int), "2", culture));
            // ConvertBack when false -> returns DoNothing
            Assert.Equal(Binding.DoNothing, converter.ConvertBack(false, typeof(int), "2", culture));
        }

        [Fact]
        public void InverseBooleanConverter_ConvertAndConvertBack_NeverThrows()
        {
            var converter = new InverseBooleanConverter();
            var culture = CultureInfo.InvariantCulture;

            Assert.False((bool)converter.Convert(true, typeof(bool), null!, culture));
            Assert.True((bool)converter.Convert(false, typeof(bool), null!, culture));

            Assert.False((bool)converter.ConvertBack(true, typeof(bool), null!, culture));
            Assert.True((bool)converter.ConvertBack(false, typeof(bool), null!, culture));
        }

        [Fact]
        public void TabToVisibilityConverter_ConvertAndConvertBack_NeverThrows()
        {
            var converter = new TabToVisibilityConverter();
            var culture = CultureInfo.InvariantCulture;

            Assert.Equal(Visibility.Visible, converter.Convert(1, typeof(Visibility), "1", culture));
            Assert.Equal(Visibility.Collapsed, converter.Convert(0, typeof(Visibility), "1", culture));

            Assert.Equal(1, converter.ConvertBack(Visibility.Visible, typeof(int), "1", culture));
            Assert.Equal(Binding.DoNothing, converter.ConvertBack(Visibility.Collapsed, typeof(int), "1", culture));
        }
    }
}
