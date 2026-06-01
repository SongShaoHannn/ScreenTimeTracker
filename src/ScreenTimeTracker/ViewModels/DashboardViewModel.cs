using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ScreenTimeTracker.Models;
using ScreenTimeTracker.Services.Abstractions;
using System.Collections.ObjectModel;

namespace ScreenTimeTracker.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWindowTrackerService _trackerService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private string _totalTodayFormatted = "0h 0m";

    [ObservableProperty]
    private string _mostUsedAppName = "暂无活动";

    [ObservableProperty]
    private int _appsTrackedCount;

    [ObservableProperty]
    private int _activeAppCount;

    [ObservableProperty]
    private ObservableCollection<DailyUsageSummary> _dailyUsageList = new();

    [ObservableProperty]
    private ObservableCollection<UsageDayPoint> _weeklyData = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasData;

    public DashboardViewModel(
        IServiceScopeFactory scopeFactory,
        IWindowTrackerService trackerService,
        IServiceProvider serviceProvider)
    {
        _scopeFactory = scopeFactory;
        _trackerService = trackerService;
        _serviceProvider = serviceProvider;

        _trackerService.AppSwitched += async (_, _) =>
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await RefreshAsync();
            });
        };

        _ = RefreshAsync();
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();

            var todayUsage = await dataService.GetDailyUsageAsync(DateTime.Today);

            var totalMinutes = todayUsage.Sum(x => x.TotalMinutes);
            TotalTodayFormatted = FormatMinutes(totalMinutes);
            MostUsedAppName = todayUsage.FirstOrDefault()?.AppName ?? "暂无活动";
            AppsTrackedCount = todayUsage.Count;
            HasData = todayUsage.Count > 0;

            var allApps = await dataService.GetAllTrackedAppsAsync();
            ActiveAppCount = allApps.Count;

            DailyUsageList = new ObservableCollection<DailyUsageSummary>(todayUsage);
            await LoadWeeklyDataAsync(dataService);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard refresh error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadWeeklyDataAsync(IDataService dataService)
    {
        var from = DateTime.Today.AddDays(-6);
        var to = DateTime.Today;
        var weekly = await dataService.GetWeeklyUsageAsync(from, to);

        WeeklyData = new ObservableCollection<UsageDayPoint>(
            weekly.Select(w => new UsageDayPoint
            {
                DayLabel = GetDayLabel(w.Date),
                Minutes = w.TotalMinutes,
                Date = w.Date,
            }));
    }

    [RelayCommand]
    private void NavigateToApp(DailyUsageSummary summary)
    {
        // Get ShellViewModel from the main window's DataContext
        var mainWindow = System.Windows.Application.Current.MainWindow;
        if (mainWindow?.DataContext is ShellViewModel shell)
        {
            shell.NavigateToAppDetailCommand.Execute(new TrackedApp
            {
                Id = summary.TrackedAppId,
                AppName = summary.AppName,
                ProcessName = summary.ProcessName,
                IconBase64 = summary.IconBase64,
                Category = summary.Category,
            });
        }
    }

    private static string GetDayLabel(DateTime date)
    {
        var days = new[] { "日", "一", "二", "三", "四", "五", "六" };
        return $"周{days[(int)date.DayOfWeek]}";
    }

    private static string FormatMinutes(double totalMinutes)
    {
        if (totalMinutes < 1) return "不足 1 分钟";

        var hours = (int)(totalMinutes / 60);
        var mins = (int)(totalMinutes % 60);

        if (hours > 0 && mins > 0)
            return $"{hours}h {mins}m";
        if (hours > 0)
            return $"{hours}h";
        return $"{mins}m";
    }
}

public class UsageDayPoint
{
    public string DayLabel { get; set; } = string.Empty;
    public double Minutes { get; set; }
    public DateTime Date { get; set; }
    public string MinutesFormatted => $"{Minutes:F0}m";

    private const double MaxBarHeight = 160.0;
    public double BarHeight
    {
        get
        {
            if (Minutes <= 0) return 4;
            var maxMinutes = 480.0;
            return Math.Max(4, Math.Min(Minutes / maxMinutes * MaxBarHeight, MaxBarHeight));
        }
    }
}
