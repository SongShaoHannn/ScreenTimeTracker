using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScreenTimeTracker.Helpers;
using ScreenTimeTracker.Models;
using ScreenTimeTracker.Services.Abstractions;
using System.Diagnostics;
using System.Text;
using System.Timers;

namespace ScreenTimeTracker.Services.Implementation;

public class WindowTrackerService : IWindowTrackerService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WindowTrackerService>? _logger;
    private System.Timers.Timer? _pollTimer;
    private AppWindowInfo? _currentForeground;
    private int? _activeSessionId;
    private bool _isTracking;
    private readonly object _lockObj = new();

    public event EventHandler<AppSwitchEventArgs>? AppSwitched;
    public bool IsTracking => _isTracking;

    public WindowTrackerService(IServiceScopeFactory scopeFactory, ILogger<WindowTrackerService>? logger = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Start()
    {
        if (_isTracking) return;

        _pollTimer = new System.Timers.Timer(1000); // Poll every 1 second
        _pollTimer.Elapsed += OnPollTimerElapsed;
        _pollTimer.AutoReset = true;
        _pollTimer.Start();
        _isTracking = true;
        _logger?.LogInformation("Window tracking started");
    }

    public void Stop()
    {
        if (!_isTracking) return;

        _pollTimer?.Stop();
        _pollTimer?.Dispose();
        _pollTimer = null;

        // End current session if any
        if (_activeSessionId.HasValue)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
                dataService.EndUsageSessionAsync(_activeSessionId.Value, DateTime.UtcNow).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error ending session on stop");
            }
            _activeSessionId = null;
        }

        _isTracking = false;
        _logger?.LogInformation("Window tracking stopped");
    }

    private async void OnPollTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            await PollForegroundAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in polling loop");
        }
    }

    private async Task PollForegroundAsync()
    {
        var current = GetForegroundAppInfo();

        // Skip invalid windows
        if (current == null || ShouldIgnore(current))
        {
            // Don't end session for brief system window flashes
            return;
        }

        lock (_lockObj)
        {
            // Check if the app actually changed
            if (_currentForeground?.ProcessName == current.ProcessName)
            {
                // Same app, update duration for active session
                return;
            }

            _currentForeground = current;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();

            // End previous session
            if (_activeSessionId.HasValue)
            {
                await dataService.EndUsageSessionAsync(_activeSessionId.Value, DateTime.UtcNow);
                _activeSessionId = null;
            }

            // Get or create tracked app
            var app = await dataService.GetOrCreateTrackedAppAsync(
                current.ProcessName, current.WindowTitle);

            // Start new session
            var session = await dataService.StartUsageSessionAsync(app.Id, DateTime.UtcNow);
            _activeSessionId = session.Id;

            _logger?.LogDebug("App switch: {ProcessName} ({AppName}) - Session {SessionId}",
                current.ProcessName, app.AppName, session.Id);

            // Notify listeners
            AppSwitched?.Invoke(this, new AppSwitchEventArgs(app, session));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing app switch to {ProcessName}", current.ProcessName);
        }
    }

    public AppWindowInfo? GetForegroundAppInfo()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
            return null;

        // Skip shell desktop
        var shellHwnd = NativeMethods.GetShellWindow();
        if (hwnd == shellHwnd)
            return null;

        // Get window title
        var sb = new StringBuilder(256);
        NativeMethods.GetWindowText(hwnd, sb, sb.Capacity);
        var windowTitle = sb.ToString();

        // Get process ID
        NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);
        if (processId == 0)
            return null;

        var processName = ProcessHelper.GetProcessName(processId);
        if (string.IsNullOrEmpty(processName))
            return null;

        return new AppWindowInfo
        {
            ProcessName = processName,
            WindowTitle = windowTitle,
            Hwnd = hwnd,
            ProcessId = processId,
        };
    }

    private static bool ShouldIgnore(AppWindowInfo info)
    {
        // Ignore processes with no meaningful window title
        if (string.IsNullOrWhiteSpace(info.WindowTitle))
            return true;

        // Ignore system processes
        if (ProcessHelper.IsSystemProcess(info.ProcessName))
            return true;

        // Ignore our own process
        if (info.ProcessName.Equals("screentimetracker.exe", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    public void Dispose()
    {
        Stop();
    }
}
