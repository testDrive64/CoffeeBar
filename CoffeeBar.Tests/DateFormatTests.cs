using System.Globalization;
using CoffeeBar.Shared;

namespace CoffeeBar.Tests;

public class DateFormatTests
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    public void GermanFormat_IsIndependentOfCulture(string culture)
    {
        var oldCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            var date = new DateTime(2023, 5, 28, 17, 30, 55);

            Assert.Equal("28.05.2023", date.ToGermanDate());
            Assert.Equal("28.05.2023 17:30", date.ToGermanDateTime());
            Assert.Equal("01.02.2024", new DateOnly(2024, 2, 1).ToGermanDate());
        }
        finally
        {
            CultureInfo.CurrentCulture = oldCulture;
        }
    }
}
