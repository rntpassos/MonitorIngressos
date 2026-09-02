namespace MonitorIngressos.Services;

public interface INotificationService
{
    Task SendAlertAsync(string message, CancellationToken ct = default);
}
