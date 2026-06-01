namespace ScreenTimeTracker.Helpers;

public record AppWindowInfo
{
    public string ProcessName { get; init; } = string.Empty;
    public string WindowTitle { get; init; } = string.Empty;
    public IntPtr Hwnd { get; init; }
    public uint ProcessId { get; init; }
}
