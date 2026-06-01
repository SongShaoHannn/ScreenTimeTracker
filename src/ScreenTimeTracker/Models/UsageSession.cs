using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScreenTimeTracker.Models;

[Table("UsageSessions")]
public class UsageSession
{
    [Key]
    public int Id { get; set; }

    public int TrackedAppId { get; set; }

    [ForeignKey(nameof(TrackedAppId))]
    public TrackedApp TrackedApp { get; set; } = null!;

    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public long DurationSeconds { get; set; }
}
