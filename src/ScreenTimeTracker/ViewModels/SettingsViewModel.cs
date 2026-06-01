using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using ScreenTimeTracker.Data;
using System.Collections.ObjectModel;
using System.IO;

namespace ScreenTimeTracker.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;

    [ObservableProperty]
    private bool _launchAtStartup;

    [ObservableProperty]
    private bool _minimizeToTray = true;

    [ObservableProperty]
    private bool _notificationsEnabled = true;

    [ObservableProperty]
    private string _quietHoursStart = "22:00";

    [ObservableProperty]
    private string _quietHoursEnd = "08:00";

    [ObservableProperty]
    private string _dbSizeFormatted = "计算中...";

    [ObservableProperty]
    private string _dbPath = string.Empty;

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private string _versionInfo = "1.0.0 — .NET 9.0, WPF"; // Version string kept technical

    public SettingsViewModel(IConfiguration config, IServiceScopeFactory scopeFactory)
    {
        _config = config;
        _scopeFactory = scopeFactory;

        // Load current settings
        LaunchAtStartup = IsStartupEnabled();
        var minimizeStr = config["Startup:StartMinimized"];
        MinimizeToTray = minimizeStr != null && bool.TryParse(minimizeStr, out var minVal) && minVal;

        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScreenTimeTracker",
            "ScreenTimeTracker.db");
        DbPath = dbPath;

        _ = CalculateDbSizeAsync();
    }

    partial void OnLaunchAtStartupChanged(bool value)
    {
        SetStartupEnabled(value);
    }

    [RelayCommand]
    private async Task ExportDataAsync()
    {
        if (IsExporting) return;
        IsExporting = true;

        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv|JSON 文件 (*.json)|*.json",
                DefaultExt = ".csv",
                FileName = $"屏幕时间_导出_{DateTime.Now:yyyyMMdd}",
            };

            if (dialog.ShowDialog() == true)
            {
                var path = dialog.FileName;
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                if (path.EndsWith(".csv"))
                {
                    await ExportToCsvAsync(db, path);
                }
                else
                {
                    await ExportToJsonAsync(db, path);
                }
            }
        }
        finally
        {
            IsExporting = false;
        }
    }

    private async Task ExportToCsvAsync(AppDbContext db, string path)
    {
        var sessions = db.UsageSessions.ToList();
        var apps = db.TrackedApps.ToDictionary(a => a.Id);

        var lines = new List<string>
        {
            "应用名称,进程名称,开始时间,结束时间,使用时长(分钟)"
        };

        foreach (var s in sessions)
        {
            var appName = apps.TryGetValue(s.TrackedAppId, out var app) ? app.AppName : "未知";
            var processName = apps.TryGetValue(s.TrackedAppId, out var app2) ? app2.ProcessName : "";
            var duration = s.DurationSeconds / 60.0;
            var endTime = s.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
            lines.Add($"\"{appName}\",\"{processName}\",{s.StartTime:yyyy-MM-dd HH:mm:ss},{endTime},{duration:F1}");
        }

        await File.WriteAllLinesAsync(path, lines);
    }

    private async Task ExportToJsonAsync(AppDbContext db, string path)
    {
        var sessions = db.UsageSessions.ToList();
        var apps = db.TrackedApps.ToDictionary(a => a.Id);

        var export = sessions.Select(s => new
        {
            AppName = apps.TryGetValue(s.TrackedAppId, out var app) ? app.AppName : "未知",
            s.StartTime,
            s.EndTime,
            DurationMinutes = s.DurationSeconds / 60.0,
        });

        var json = System.Text.Json.JsonSerializer.Serialize(export, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
        });

        await File.WriteAllTextAsync(path, json);
    }

    [RelayCommand]
    private async Task ClearDataAsync()
    {
        var result = System.Windows.MessageBox.Show(
            "确定要清除所有使用记录吗？此操作不可撤销。",
            "清除所有数据",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
            return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.UsageSessions.RemoveRange(db.UsageSessions);
        db.TimeLimits.RemoveRange(db.TimeLimits);
        db.TrackedApps.RemoveRange(db.TrackedApps);
        await db.SaveChangesAsync();

        await CalculateDbSizeAsync();
    }

    private async Task CalculateDbSizeAsync()
    {
        try
        {
            if (File.Exists(DbPath))
            {
                var info = new FileInfo(DbPath);
                var sizeMb = info.Length / (1024.0 * 1024.0);
                DbSizeFormatted = $"{sizeMb:F1} MB";
            }
            else
            {
                DbSizeFormatted = "0 MB"; // Just the number
            }
        }
        catch
        {
            DbSizeFormatted = "未知";
        }

        await Task.CompletedTask;
    }

    private bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue("ScreenTimeTracker") != null;
        }
        catch
        {
            return false;
        }
    }

    private void SetStartupEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;

            if (enabled)
            {
                var exePath = Environment.ProcessPath ?? "";
                key.SetValue("ScreenTimeTracker", $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue("ScreenTimeTracker", false);
            }
        }
        catch
        {
            // Ignore registry errors
        }
    }
}
