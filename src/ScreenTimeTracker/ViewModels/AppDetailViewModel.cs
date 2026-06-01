using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ScreenTimeTracker.Models;
using ScreenTimeTracker.Services.Abstractions;
using System.Collections.ObjectModel;

namespace ScreenTimeTracker.ViewModels;

public partial class AppDetailViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private int _trackedAppId;

    [ObservableProperty]
    private string _appName = string.Empty;

    [ObservableProperty]
    private string _processName = string.Empty;

    [ObservableProperty]
    private string? _iconBase64;

    [ObservableProperty]
    private string _category = string.Empty;

    [ObservableProperty]
    private string _todayUsageFormatted = "0h 0m";

    [ObservableProperty]
    private double _todayUsageMinutes;

    [ObservableProperty]
    private bool _hasTimeLimit;

    [ObservableProperty]
    private int _dailyLimitMinutes;

    [ObservableProperty]
    private bool _limitEnabled;

    [ObservableProperty]
    private int _limitHours;

    [ObservableProperty]
    private int _limitMinutesPart;

    [ObservableProperty]
    private double _usageRatio;

    [ObservableProperty]
    private string _usageRatioText = "0%";

    [ObservableProperty]
    private ObservableCollection<DailyUsageSummary> _weeklyUsage = new();

    [ObservableProperty]
    private ObservableCollection<UsageSessionInfo> _todaySessions = new();

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private bool _isLoading;

    public AppDetailViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void Initialize(TrackedApp app)
    {
        _trackedAppId = app.Id;
        AppName = app.AppName;
        ProcessName = app.ProcessName;
        IconBase64 = app.IconBase64;
        Category = app.Category ?? "未分类";

        _ = LoadDataAsync();
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();

            // Get app with time limit
            var app = await dataService.GetTrackedAppAsync(_trackedAppId);
            if (app == null) return;

            AppName = app.AppName;
            IconBase64 = app.IconBase64;
            Category = app.Category ?? "未分类";

            // Time limit
            if (app.TimeLimit != null)
            {
                HasTimeLimit = true;
                DailyLimitMinutes = app.TimeLimit.DailyLimitMinutes;
                LimitEnabled = app.TimeLimit.IsEnabled;
                LimitHours = app.TimeLimit.DailyLimitMinutes / 60;
                LimitMinutesPart = app.TimeLimit.DailyLimitMinutes % 60;
            }
            else
            {
                HasTimeLimit = false;
                LimitEnabled = false;
                DailyLimitMinutes = 0;
                LimitHours = 0;
                LimitMinutesPart = 0;
            }

            // Today's usage
            var todayUsage = await dataService.GetDailyUsageAsync(DateTime.Today);
            var today = todayUsage.FirstOrDefault(u => u.TrackedAppId == _trackedAppId);
            if (today != null)
            {
                TodayUsageMinutes = today.TotalMinutes;
                TodayUsageFormatted = FormatMinutes(today.TotalMinutes);

                if (LimitEnabled && DailyLimitMinutes > 0)
                {
                    UsageRatio = Math.Min(today.TotalMinutes / DailyLimitMinutes, 1.0);
                    UsageRatioText = $"{(int)(UsageRatio * 100)}%";
                }
                else
                {
                    UsageRatio = 0;
                    UsageRatioText = "未设置限制";
                }
            }
            else
            {
                TodayUsageMinutes = 0;
                TodayUsageFormatted = "0m";
                UsageRatio = 0;
                UsageRatioText = "未设置限制";
            }

            // Weekly usage
            var weekly = await dataService.GetWeeklyUsageAsync(DateTime.Today.AddDays(-6), DateTime.Today);
            // For this app specifically, we'd need a per-app weekly query
            // Simplified: show today's sessions
            var sessions = await dataService.GetSessionsForAppAsync(_trackedAppId, DateTime.Today);
            TodaySessions = new ObservableCollection<UsageSessionInfo>(
                sessions.Select(s => new UsageSessionInfo
                {
                    StartTime = s.StartTime.ToLocalTime(),
                    EndTime = s.EndTime?.ToLocalTime(),
                    Duration = s.EndTime.HasValue
                        ? FormatMinutes((s.EndTime.Value - s.StartTime).TotalMinutes)
                        : "使用中",
                    DurationMinutes = s.EndTime.HasValue
                        ? (s.EndTime.Value - s.StartTime).TotalMinutes
                        : (DateTime.UtcNow - s.StartTime).TotalMinutes,
                }));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"App detail load error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveLimitAsync()
    {
        if (IsSaving) return;
        IsSaving = true;

        try
        {
            var totalMinutes = LimitHours * 60 + LimitMinutesPart;
            if (totalMinutes < 0) totalMinutes = 0;

            using var scope = _scopeFactory.CreateScope();
            var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
            await dataService.SetTimeLimitAsync(_trackedAppId, totalMinutes, LimitEnabled);

            DailyLimitMinutes = totalMinutes;
            HasTimeLimit = LimitEnabled;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Save limit error: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void ToggleLimit()
    {
        LimitEnabled = !LimitEnabled;
    }

    private static string FormatMinutes(double totalMinutes)
    {
        if (totalMinutes < 1) return "不足 1 分钟";

        var hours = (int)(totalMinutes / 60);
        var mins = (int)(totalMinutes % 60);

        if (hours > 0 && mins > 0) return $"{hours}h {mins}m";
        if (hours > 0) return $"{hours}h";
        return $"{mins}m";
    }
}

public class UsageSessionInfo
{
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Duration { get; set; } = string.Empty;
    public double DurationMinutes { get; set; }
    public string TimeRange => EndTime.HasValue
        ? $"{StartTime:HH:mm} - {EndTime:HH:mm}"
        : $"{StartTime:HH:mm} - 至今";
}
