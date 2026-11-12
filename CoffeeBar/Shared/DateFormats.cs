using System.Globalization;

namespace CoffeeBar.Shared;

// German date format (28.05.2023) for the whole app, independent of the server culture.
public static class DateFormats
{
    public const string Date = "dd.MM.yyyy";
    public const string DateTime = "dd.MM.yyyy HH:mm";

    // date-fns equivalent of Date, used by the charts (Chart.js date adapter).
    public const string ChartDate = "dd.MM.yyyy";

    public static string ToGermanDate(this System.DateTime value) =>
        value.ToString(Date, CultureInfo.InvariantCulture);

    public static string ToGermanDate(this DateOnly value) =>
        value.ToString(Date, CultureInfo.InvariantCulture);

    public static string ToGermanDateTime(this System.DateTime value) =>
        value.ToString(DateTime, CultureInfo.InvariantCulture);
}
