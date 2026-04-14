namespace AzureSummary.Models;

public class QuietHoursConfig
{
    public bool Enabled { get; set; } = true;
    public TimeOnly Start { get; set; } = new TimeOnly(18, 0);
    public TimeOnly End { get; set; } = new TimeOnly(8, 0);
    public string TimeZoneId { get; set; } = "Asia/Taipei";

    public bool IsActive()
    {
        if (!Enabled) return false;

        var tz = ResolveTimeZone();
        var localTime = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));

        // Spans midnight (e.g. 18:00 → 08:00): active if time >= start OR time < end
        if (Start > End)
            return localTime >= Start || localTime < End;

        return localTime >= Start && localTime < End;
    }

    public string LocalTimeZoneAbbreviation()
    {
        var tz = ResolveTimeZone();
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        // Use IsDaylightSavingTime to choose abbreviation if available
        return tz.IsDaylightSavingTime(local)
            ? tz.DaylightName
            : tz.StandardName;
    }

    private TimeZoneInfo ResolveTimeZone()
    {
        // .NET 6+ resolves both IANA (Asia/Taipei) and Windows (Taipei Standard Time) IDs
        try { return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId); }
        catch { return TimeZoneInfo.Utc; }
    }
}
