namespace ScreenTimeTracker.Models;

public class DailyUsageSummary
{
    public int TrackedAppId { get; set; }
    public string AppName { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? IconBase64 { get; set; }
    public DateTime Date { get; set; }
    public double TotalMinutes { get; set; }
    public int? LimitMinutes { get; set; }
    public int ActiveSessionId { get; set; }
    public DateTime? SessionStartTime { get; set; }

    // Computed properties for UI binding
    public double UsageRatio => LimitMinutes.HasValue && LimitMinutes.Value > 0
        ? Math.Min(TotalMinutes / LimitMinutes.Value, 1.0)
        : 0;

    public string UsageRatioText => LimitMinutes.HasValue && LimitMinutes.Value > 0
        ? $"{(int)(UsageRatio * 100)}%"
        : "";

    public string UsageTimeFormatted
    {
        get
        {
            var hours = (int)(TotalMinutes / 60);
            var mins = (int)(TotalMinutes % 60);
            if (hours > 0 && mins > 0) return $"{hours}h {mins}m";
            if (hours > 0) return $"{hours}h";
            return $"{mins}m";
        }
    }
}
