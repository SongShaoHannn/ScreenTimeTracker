using ScreenTimeTracker.Helpers;
using ScreenTimeTracker.Models;

namespace ScreenTimeTracker.Services.Abstractions;

public class AppSwitchEventArgs : EventArgs
{
    public TrackedApp App { get; }
    public UsageSession Session { get; }

    public AppSwitchEventArgs(TrackedApp app, UsageSession session)
    {
        App = app;
        Session = session;
    }
}

public interface IWindowTrackerService
{
    event EventHandler<AppSwitchEventArgs>? AppSwitched;
    void Start();
    void Stop();
    AppWindowInfo? GetForegroundAppInfo();
    bool IsTracking { get; }
}
