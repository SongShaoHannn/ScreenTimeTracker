using Microsoft.Extensions.Logging;
using ScreenTimeTracker.Services.Abstractions;

namespace ScreenTimeTracker.Services.Implementation;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService>? _logger;
    private readonly HashSet<string> _recentlyNotified = new();
    private DateTime _lastCleanup = DateTime.UtcNow;

    public NotificationService(ILogger<NotificationService>? logger = null)
    {
        _logger = logger;
    }

    public void SendLimitExceeded(string appName, int limitMinutes)
    {
        var key = $"exceeded_{appName}_{DateTime.Today:yyyyMMdd}";
        if (_recentlyNotified.Contains(key))
            return;

        _recentlyNotified.Add(key);
        CleanupOldNotifications();

        var title = "时间限额已用完";
        var message = $"你今天使用 {appName} 已超过 {limitMinutes} 分钟。休息一下吧。";

        SendWindowsToast(title, message);
        _logger?.LogInformation("Limit exceeded notification: {AppName} ({LimitMinutes}m)", appName, limitMinutes);
    }

    public void SendWarning(string appName, double usageRatio, int limitMinutes)
    {
        var key = $"warning_{appName}_{DateTime.Today:yyyyMMdd}";
        if (_recentlyNotified.Contains(key))
            return;

        _recentlyNotified.Add(key);
        CleanupOldNotifications();

        var percentUsed = (int)(usageRatio * 100);
        var title = "使用提醒";
        var message = $"你已使用 {appName} {limitMinutes} 分钟限额的 {percentUsed}%。";

        SendWindowsToast(title, message);
        _logger?.LogInformation("Warning notification: {AppName} at {Percent}%", appName, percentUsed);
    }

    public void SendInfo(string title, string message)
    {
        SendWindowsToast(title, message);
    }

    private void SendWindowsToast(string title, string message)
    {
        try
        {
            // Use PowerShell to show toast notification
            // This works without UWP packaging requirements
            var escapedTitle = title.Replace("\"", "\\\"").Replace("'", "\\'");
            var escapedMessage = message.Replace("\"", "\\\"").Replace("'", "\\'");

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -Command \"[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null; $template = [Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent([Windows.UI.Notifications.ToastTemplateType]::ToastText02); $textNodes = $template.GetElementsByTagName('text'); $textNodes.Item(0).AppendChild($template.CreateTextNode('{escapedTitle}')) | Out-Null; $textNodes.Item(1).AppendChild($template.CreateTextNode('{escapedMessage}')) | Out-Null; $notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('ScreenTimeTracker'); $toast = New-Object Windows.UI.Notifications.ToastNotification($template); $notifier.Show($toast);\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to send toast notification");
            // Fallback: log the notification
            _logger?.LogInformation("NOTIFICATION: {Title} - {Message}", title, message);
        }
    }

    private void CleanupOldNotifications()
    {
        // Clean up old keys once per hour
        if ((DateTime.UtcNow - _lastCleanup).TotalHours < 1)
            return;

        var todayKey = DateTime.Today.ToString("yyyyMMdd");
        var toRemove = _recentlyNotified.Where(k => !k.EndsWith(todayKey)).ToList();
        foreach (var key in toRemove)
        {
            _recentlyNotified.Remove(key);
        }

        _lastCleanup = DateTime.UtcNow;
    }
}
