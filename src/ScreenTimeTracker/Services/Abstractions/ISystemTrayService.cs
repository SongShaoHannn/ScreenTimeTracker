using System.Windows;

namespace ScreenTimeTracker.Services.Abstractions;

public interface ISystemTrayService
{
    void Initialize(Window mainWindow);
    void ShowBalloonTip(string title, string message);
    void Dispose();
}
