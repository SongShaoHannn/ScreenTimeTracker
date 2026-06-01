using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScreenTimeTracker.Data;
using ScreenTimeTracker.Services.Abstractions;
using ScreenTimeTracker.Services.Implementation;
using ScreenTimeTracker.ViewModels;
using System.IO;
using System.Windows;

namespace ScreenTimeTracker;

public partial class App : Application
{
    public IServiceProvider Services { get; }

    public App()
    {
        DispatcherUnhandledException += (s, e) =>
        {
            MessageBox.Show($"错误: {e.Exception.Message}", "屏幕时间管家",
                MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        services.AddSingleton<IConfiguration>(config);

        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScreenTimeTracker", "ScreenTimeTracker.db");
        var dbDir = Path.GetDirectoryName(dbPath);
        if (dbDir is not null && !Directory.Exists(dbDir))
            Directory.CreateDirectory(dbDir);

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        services.AddSingleton<IWindowTrackerService, WindowTrackerService>();
        services.AddSingleton<IDataService, DataService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<ILimitCheckService, LimitCheckService>();
        services.AddSingleton<ISystemTrayService, SystemTrayService>();

        services.AddSingleton<ShellViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<AppDetailViewModel>();
        services.AddTransient<SettingsViewModel>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            using (var scope = Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
            }

            var tracker = Services.GetRequiredService<IWindowTrackerService>();
            tracker.Start();

            var limitChecker = Services.GetRequiredService<ILimitCheckService>();
            limitChecker.Start();

            var shell = Services.GetRequiredService<ShellViewModel>();
            var window = new MainWindow { DataContext = shell };
            window.Show();

            var trayService = Services.GetRequiredService<ISystemTrayService>();
            trayService.Initialize(window);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"启动失败:\n{ex.Message}", "屏幕时间管家",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { Services.GetRequiredService<IWindowTrackerService>().Stop(); } catch { }
        base.OnExit(e);
    }
}
