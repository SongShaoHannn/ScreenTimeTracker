using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScreenTimeTracker.Services.Abstractions;
using System.Timers;

namespace ScreenTimeTracker.Services.Implementation;

public class LimitCheckService : ILimitCheckService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INotificationService _notificationService;
    private readonly ILogger<LimitCheckService>? _logger;
    private System.Timers.Timer? _checkTimer;
    private readonly Dictionary<int, bool> _alreadyExceededToday = new();
    private DateTime _lastDateCheck = DateTime.Today;

    public LimitCheckService(
        IServiceScopeFactory scopeFactory,
        INotificationService notificationService,
        ILogger<LimitCheckService>? logger = null)
    {
        _scopeFactory = scopeFactory;
        _notificationService = notificationService;
        _logger = logger;
    }

    public void Start()
    {
        _checkTimer = new System.Timers.Timer(60000); // Check every 60 seconds
        _checkTimer.Elapsed += OnCheckTimerElapsed;
        _checkTimer.AutoReset = true;
        _checkTimer.Start();
        _logger?.LogInformation("Limit check service started");
    }

    public void Stop()
    {
        _checkTimer?.Stop();
        _checkTimer?.Dispose();
        _checkTimer = null;
    }

    private async void OnCheckTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            await EvaluateAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error evaluating limits");
        }
    }

    public async Task EvaluateAsync()
    {
        // Reset notifications if day changed
        if (DateTime.Today != _lastDateCheck)
        {
            _alreadyExceededToday.Clear();
            _lastDateCheck = DateTime.Today;
        }

        using var scope = _scopeFactory.CreateScope();
        var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();

        var todayUsage = await dataService.GetDailyUsageAsync(DateTime.Today);

        foreach (var usage in todayUsage.Where(u => u.LimitMinutes.HasValue && u.LimitMinutes.Value > 0))
        {
            if (!usage.LimitMinutes.HasValue) continue;
            var limitMinutes = usage.LimitMinutes.Value;
            var ratio = usage.TotalMinutes / limitMinutes;

            if (ratio >= 1.0)
            {
                if (!_alreadyExceededToday.ContainsKey(usage.TrackedAppId))
                {
                    _notificationService.SendLimitExceeded(usage.AppName, limitMinutes);
                    _alreadyExceededToday[usage.TrackedAppId] = true;
                    _logger?.LogInformation("Limit exceeded for {AppName}: {Used:F1}m / {Limit}m",
                        usage.AppName, usage.TotalMinutes, limitMinutes);
                }
            }
            else if (ratio >= 0.8)
            {
                // Send warning at 80% threshold (only once)
                if (!_alreadyExceededToday.ContainsKey(usage.TrackedAppId))
                {
                    _notificationService.SendWarning(usage.AppName, ratio, limitMinutes);
                    _logger?.LogInformation("Warning for {AppName}: {Percent}% ({Used:F1}m / {Limit}m)",
                        usage.AppName, (int)(ratio * 100), usage.TotalMinutes, limitMinutes);
                }
            }
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
