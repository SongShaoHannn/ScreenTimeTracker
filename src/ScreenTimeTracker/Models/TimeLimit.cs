using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScreenTimeTracker.Models;

[Table("TimeLimits")]
public class TimeLimit
{
    [Key]
    public int Id { get; set; }

    public int TrackedAppId { get; set; }

    [ForeignKey(nameof(TrackedAppId))]
    public TrackedApp TrackedApp { get; set; } = null!;

    public int DailyLimitMinutes { get; set; }
    public bool IsEnabled { get; set; }
    public double WarningThreshold { get; set; } = 0.8;
}
