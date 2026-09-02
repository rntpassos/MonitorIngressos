namespace MonitorIngressos.Services;

public interface IScraperService
{
    Task<bool> HasTicketsAvailableAsync(CancellationToken ct = default);
}
