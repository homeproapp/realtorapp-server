namespace RealtorApp.Domain.Extensions;

public static class DateTimeExtensions
{
    public static DateTime AddDaysAndSetToEndOfDay(this DateTime dateTime, int days)
    {
        return dateTime.Date.AddDays(days + 1).AddTicks(-1);
    }
}
