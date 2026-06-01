using ScreenTimeTracker.Models;

namespace ScreenTimeTracker.Services.Abstractions;

public interface IDataService
{
    // TrackedApp
    Task<TrackedApp> GetOrCreateTrackedAppAsync(string processName, string windowTitle);
    Task<TrackedApp?> GetTrackedAppAsync(int id);
    Task<List<TrackedApp>> GetAllTrackedAppsAsync();
    Task<TrackedApp?> GetTrackedAppByProcessNameAsync(string processName);

    // UsageSession
    Task<UsageSession> StartUsageSessionAsync(int trackedAppId, DateTime startTime);
    Task EndUsageSessionAsync(int sessionId, DateTime endTime);
    Task<List<UsageSession>> GetSessionsForAppAsync(int trackedAppId, DateTime date);
    Task<UsageSession?> GetActiveSessionAsync();

    // Queries
    Task<List<DailyUsageSummary>> GetDailyUsageAsync(DateTime date);
    Task<List<DailyUsageSummary>> GetWeeklyUsageAsync(DateTime from, DateTime to);

    // TimeLimit
    Task<TimeLimit?> GetTimeLimitAsync(int trackedAppId);
    Task SetTimeLimitAsync(int trackedAppId, int dailyLimitMinutes, bool isEnabled, double warningThreshold = 0.8);

    // Settings
    Task SaveSettingsAsync(Dictionary<string, string> settings);
    Task<Dictionary<string, string>> LoadSettingsAsync();
}
