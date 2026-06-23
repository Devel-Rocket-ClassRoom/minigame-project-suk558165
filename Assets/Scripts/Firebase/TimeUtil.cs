using System;

public static class TimeUtil
{
    public static long NowUnixMillis()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public static DateTime FromUnixMillis(long millis)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(millis).LocalDateTime;
    }

    public static string ToDateString(long millis)
    {
        return FromUnixMillis(millis).ToString("yyyy-MM-dd HH:mm:ss");
    }
}
