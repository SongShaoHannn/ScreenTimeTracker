namespace ScreenTimeTracker.Services.Abstractions;

public interface INotificationService
{
    void SendLimitExceeded(string appName, int limitMinutes);
    void SendWarning(string appName, double usageRatio, int limitMinutes);
    void SendInfo(string title, string message);
}
