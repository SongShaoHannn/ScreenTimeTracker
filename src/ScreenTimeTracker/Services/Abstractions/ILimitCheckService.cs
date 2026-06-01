namespace ScreenTimeTracker.Services.Abstractions;

public interface ILimitCheckService
{
    void Start();
    void Stop();
    Task EvaluateAsync();
}
