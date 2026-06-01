using Hardcodet.Wpf.TaskbarNotification;
using ScreenTimeTracker.Services.Abstractions;
using System.IO;
using System.Windows;

namespace ScreenTimeTracker.Services.Implementation;

public class SystemTrayService : ISystemTrayService
{
    private TaskbarIcon? _trayIcon;
    private bool _disposed;

    public void Initialize(Window mainWindow)
    {
        // Try to load tray icon, or create a default one
        System.Drawing.Icon? icon = null;
        try
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "tray-icon.ico");
            if (File.Exists(iconPath))
            {
                icon = new System.Drawing.Icon(iconPath);
            }
        }
        catch
        {
            // Icon file not found or invalid
        }

        _trayIcon = new TaskbarIcon
        {
            Icon = icon ?? System.Drawing.SystemIcons.Application,
            ToolTipText = "屏幕时间管家",
            Visibility = Visibility.Visible,
        };

        // Context menu
        var contextMenu = new System.Windows.Controls.ContextMenu();

        var openItem = new System.Windows.Controls.MenuItem { Header = "打开仪表盘" };
        openItem.Click += (_, _) =>
        {
            mainWindow.Show();
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
        };
        contextMenu.Items.Add(openItem);

        contextMenu.Items.Add(new System.Windows.Controls.Separator());

        var quitItem = new System.Windows.Controls.MenuItem { Header = "退出屏幕时间管家" };
        quitItem.Click += (_, _) =>
        {
            _trayIcon?.Dispose();
            Application.Current.Shutdown();
        };
        contextMenu.Items.Add(quitItem);

        _trayIcon.ContextMenu = contextMenu;

        // Double-click to show window
        _trayIcon.TrayMouseDoubleClick += (_, _) =>
        {
            mainWindow.Show();
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
        };

        // Minimize to tray instead of closing
        mainWindow.Closing += (_, args) =>
        {
            args.Cancel = true;
            mainWindow.Hide();
        };

        // Clean up on window close (actual shutdown)
        mainWindow.Closed += (_, _) =>
        {
            if (!_disposed)
            {
                _trayIcon?.Dispose();
                _disposed = true;
            }
        };
    }

    public void ShowBalloonTip(string title, string message)
    {
        _trayIcon?.ShowBalloonTip(title, message, BalloonIcon.Info);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _trayIcon?.Dispose();
            _disposed = true;
        }
    }
}
