namespace RealtorApp.Domain.Helpers;

public static class SequentialGuidGenerator
{
    // Generates a new sequential GUID, optimized for SQL Server.
    // The timestamp part is placed at the end of the GUID to ensure SQL Server's
    // uniqueidentifier type sorts it correctly.
    public static Guid NewSequentialGuid()
    {
        byte[] guidBytes = Guid.NewGuid().ToByteArray();

        DateTime now = DateTime.UtcNow;
        DateTime baseDate = new DateTime(1900, 1, 1); // SQL Server's base date for datetime
        TimeSpan timeSpan = new TimeSpan(now.Ticks - baseDate.Ticks);
        byte[] timeBytes = BitConverter.GetBytes(timeSpan.TotalDays); // Days since 1900-01-01
        byte[] daysBytes = BitConverter.GetBytes(now.TimeOfDay.TotalMilliseconds); // Milliseconds since midnight

        // Reverse the byte order for timeBytes and daysBytes to ensure correct sorting in SQL Server
        Array.Reverse(timeBytes);
        Array.Reverse(daysBytes);

        // Copy the time components to the end of the GUID byte array
        // This specific byte arrangement is crucial for SQL Server's uniqueidentifier sorting
        Array.Copy(timeBytes, 0, guidBytes, 10, 6); // Copy 6 bytes of days
        Array.Copy(daysBytes, 0, guidBytes, 8, 2); // Copy 2 bytes of milliseconds (part of it)

        return new Guid(guidBytes);
    }
}