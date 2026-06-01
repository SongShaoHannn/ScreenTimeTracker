using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ScreenTimeTracker.Models;

namespace ScreenTimeTracker.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly IServiceProvider _services;
    private System.Timers.Timer? _refreshTimer;

    [ObservableProperty]
    private ObservableObject _currentPage = null!;

    [ObservableProperty]
    private string _selectedNavItem = "仪表盘"; // 仪表盘

    [ObservableProperty]
    private bool _isTrackingPaused;

    public ShellViewModel(IServiceProvider services)
    {
        _services = services;
        CurrentPage = _services.GetRequiredService<DashboardViewModel>();

        // Set up periodic refresh
        _refreshTimer = new System.Timers.Timer(5000); // Refresh every 5 seconds
        _refreshTimer.Elapsed += async (_, _) => await RefreshCurrentPageAsync();
        _refreshTimer.AutoReset = true;
        _refreshTimer.Start();
    }

    private async Task RefreshCurrentPageAsync()
    {
        try
        {
            if (CurrentPage is DashboardViewModel dashboard)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await dashboard.RefreshAsync();
                });
            }
            else if (CurrentPage is AppDetailViewModel detail)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await detail.RefreshAsync();
                });
            }
        }
        catch
        {
            // Ignore refresh errors
        }
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        SelectedNavItem = "仪表盘";
        CurrentPage = _services.GetRequiredService<DashboardViewModel>();
    }

    [RelayCommand]
    private void NavigateToAppDetail(TrackedApp app)
    {
        SelectedNavItem = "";
        var vm = _services.GetRequiredService<AppDetailViewModel>();
        vm.Initialize(app);
        CurrentPage = vm;
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        SelectedNavItem = "设置";
        CurrentPage = _services.GetRequiredService<SettingsViewModel>();
    }
}
