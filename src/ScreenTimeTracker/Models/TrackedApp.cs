using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScreenTimeTracker.Models;

[Table("TrackedApps")]
public class TrackedApp
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(256)]
    public string ProcessName { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string AppName { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? Category { get; set; }

    public string? IconBase64 { get; set; }
    public string? IconColor { get; set; }
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public ICollection<UsageSession> UsageSessions { get; set; } = new List<UsageSession>();
    public TimeLimit? TimeLimit { get; set; }
}
