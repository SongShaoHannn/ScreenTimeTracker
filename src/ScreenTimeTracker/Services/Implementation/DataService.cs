using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScreenTimeTracker.Data;
using ScreenTimeTracker.Helpers;
using ScreenTimeTracker.Models;
using ScreenTimeTracker.Services.Abstractions;

namespace ScreenTimeTracker.Services.Implementation;

public class DataService : IDataService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DataService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    private AppDbContext CreateContext()
    {
        var scope = _scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    // TrackedApp operations
    public async Task<TrackedApp> GetOrCreateTrackedAppAsync(string processName, string windowTitle)
    {
        using var ctx = CreateContext();
        var app = await ctx.TrackedApps
            .Include(a => a.TimeLimit)
            .FirstOrDefaultAsync(a => a.ProcessName == processName);

        if (app == null)
        {
            var friendlyName = ProcessHelper.GetFriendlyName(processName);
            var icon = IconHelper.ExtractIconAsBase64(processName);

            app = new TrackedApp
            {
                ProcessName = processName,
                AppName = friendlyName,
                FirstSeenAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
                IconBase64 = icon,
                IsActive = true,
            };

            ctx.TrackedApps.Add(app);
            await ctx.SaveChangesAsync();
        }
        else
        {
            // Update last seen
            app.LastSeenAt = DateTime.UtcNow;
            if (!app.IsActive) app.IsActive = true;

            // Update icon if it was missing
            if (string.IsNullOrEmpty(app.IconBase64))
            {
                app.IconBase64 = IconHelper.ExtractIconAsBase64(processName);
            }

            await ctx.SaveChangesAsync();
        }

        return app;
    }

    public async Task<TrackedApp?> GetTrackedAppAsync(int id)
    {
        using var ctx = CreateContext();
        return await ctx.TrackedApps
            .Include(a => a.TimeLimit)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<TrackedApp>> GetAllTrackedAppsAsync()
    {
        using var ctx = CreateContext();
        return await ctx.TrackedApps
            .Include(a => a.TimeLimit)
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.LastSeenAt)
            .ToListAsync();
    }

    public async Task<TrackedApp?> GetTrackedAppByProcessNameAsync(string processName)
    {
        using var ctx = CreateContext();
        return await ctx.TrackedApps
            .Include(a => a.TimeLimit)
            .FirstOrDefaultAsync(a => a.ProcessName == processName);
    }

    // UsageSession operations
    public async Task<UsageSession> StartUsageSessionAsync(int trackedAppId, DateTime startTime)
    {
        using var ctx = CreateContext();
        var session = new UsageSession
        {
            TrackedAppId = trackedAppId,
            StartTime = startTime,
            EndTime = null,
            DurationSeconds = 0,
        };

        ctx.UsageSessions.Add(session);
        await ctx.SaveChangesAsync();
        return session;
    }

    public async Task EndUsageSessionAsync(int sessionId, DateTime endTime)
    {
        using var ctx = CreateContext();
        var session = await ctx.UsageSessions.FindAsync(sessionId);
        if (session == null || session.EndTime.HasValue)
            return;

        session.EndTime = endTime;
        session.DurationSeconds = (long)(endTime - session.StartTime).TotalSeconds;

        // Cap unreasonable durations (e.g., if clock changed)
        if (session.DurationSeconds > 86400)
            session.DurationSeconds = 86400;

        await ctx.SaveChangesAsync();
    }

    public async Task<List<UsageSession>> GetSessionsForAppAsync(int trackedAppId, DateTime date)
    {
        using var ctx = CreateContext();
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        return await ctx.UsageSessions
            .Where(s => s.TrackedAppId == trackedAppId
                        && s.StartTime >= dayStart
                        && s.StartTime < dayEnd)
            .OrderBy(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<UsageSession?> GetActiveSessionAsync()
    {
        using var ctx = CreateContext();
        return await ctx.UsageSessions
            .Include(s => s.TrackedApp)
            .Where(s => s.EndTime == null)
            .OrderByDescending(s => s.StartTime)
            .FirstOrDefaultAsync();
    }

    // Query operations
    public async Task<List<DailyUsageSummary>> GetDailyUsageAsync(DateTime date)
    {
        using var ctx = CreateContext();
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        var sessions = await ctx.UsageSessions
            .Include(s => s.TrackedApp)
            .ThenInclude(a => a.TimeLimit)
            .Where(s => s.StartTime >= dayStart && s.StartTime < dayEnd)
            .ToListAsync();

        // Also include currently active session
        var activeSession = await ctx.UsageSessions
            .Include(s => s.TrackedApp)
            .ThenInclude(a => a.TimeLimit)
            .Where(s => s.EndTime == null && s.StartTime >= dayStart && s.StartTime < dayEnd)
            .FirstOrDefaultAsync();

        var grouped = sessions
            .GroupBy(s => s.TrackedAppId)
            .Select(g =>
            {
                var first = g.First();
                var totalSeconds = g.Sum(s => s.DurationSeconds);

                // If there's an active session for this app, add current elapsed time
                if (activeSession != null && activeSession.TrackedAppId == g.Key)
                {
                    totalSeconds += (long)(DateTime.UtcNow - activeSession.StartTime).TotalSeconds;
                }

                return new DailyUsageSummary
                {
                    TrackedAppId = g.Key,
                    AppName = first.TrackedApp.AppName,
                    ProcessName = first.TrackedApp.ProcessName,
                    Category = first.TrackedApp.Category,
                    IconBase64 = first.TrackedApp.IconBase64,
                    Date = date.Date,
                    TotalMinutes = totalSeconds / 60.0,
                    LimitMinutes = first.TrackedApp.TimeLimit?.DailyLimitMinutes,
                    ActiveSessionId = activeSession?.TrackedAppId == g.Key ? activeSession.Id : 0,
                    SessionStartTime = activeSession?.TrackedAppId == g.Key ? activeSession.StartTime : null,
                };
            })
            .OrderByDescending(x => x.TotalMinutes)
            .ToList();

        return grouped;
    }

    public async Task<List<DailyUsageSummary>> GetWeeklyUsageAsync(DateTime from, DateTime to)
    {
        using var ctx = CreateContext();
        var sessions = await ctx.UsageSessions
            .Include(s => s.TrackedApp)
            .Where(s => s.StartTime >= from.Date && s.StartTime < to.Date.AddDays(1))
            .ToListAsync();

        // Group by day
        var result = new List<DailyUsageSummary>();
        for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
        {
            var dayStart = date;
            var dayEnd = date.AddDays(1);

            var daySessions = sessions
                .Where(s => s.StartTime >= dayStart && s.StartTime < dayEnd)
                .ToList();

            var totalSeconds = daySessions.Sum(s => s.DurationSeconds);

            result.Add(new DailyUsageSummary
            {
                Date = date,
                AppName = "所有应用",
                ProcessName = "",
                TotalMinutes = totalSeconds / 60.0,
            });
        }

        return result;
    }

    // TimeLimit operations
    public async Task<TimeLimit?> GetTimeLimitAsync(int trackedAppId)
    {
        using var ctx = CreateContext();
        return await ctx.TimeLimits
            .FirstOrDefaultAsync(t => t.TrackedAppId == trackedAppId);
    }

    public async Task SetTimeLimitAsync(int trackedAppId, int dailyLimitMinutes, bool isEnabled, double warningThreshold = 0.8)
    {
        using var ctx = CreateContext();
        var limit = await ctx.TimeLimits
            .FirstOrDefaultAsync(t => t.TrackedAppId == trackedAppId);

        if (limit == null)
        {
            limit = new TimeLimit
            {
                TrackedAppId = trackedAppId,
                DailyLimitMinutes = dailyLimitMinutes,
                IsEnabled = isEnabled,
                WarningThreshold = warningThreshold,
            };
            ctx.TimeLimits.Add(limit);
        }
        else
        {
            limit.DailyLimitMinutes = dailyLimitMinutes;
            limit.IsEnabled = isEnabled;
            limit.WarningThreshold = warningThreshold;
        }

        await ctx.SaveChangesAsync();
    }

    // Settings operations
    public Task SaveSettingsAsync(Dictionary<string, string> settings)
    {
        // For now, settings are stored in appsettings.json and managed via IConfiguration
        // Future: persist to a Settings table
        return Task.CompletedTask;
    }

    public Task<Dictionary<string, string>> LoadSettingsAsync()
    {
        return Task.FromResult(new Dictionary<string, string>());
    }
}
