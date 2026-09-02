using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitorIngressos.Configuration;
using MonitorIngressos.Services;

namespace MonitorIngressos;

public class Worker : BackgroundService
{
    private readonly IScraperService _scraperService;
    private readonly INotificationService _notificationService;
    private readonly MonitorSettings _settings;
    private readonly ILogger<Worker> _logger;
    private bool _wereTicketsAvailable = false;

    public Worker(
        IScraperService scraperService,
        INotificationService notificationService,
        IOptions<MonitorSettings> settings,
        ILogger<Worker> logger)
    {
        _scraperService = scraperService;
        _notificationService = notificationService;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço Monitor de Ingressos iniciado. Checando a cada {Interval} segundos.", _settings.IntervalSeconds);

        string startupMessage = "🟢 *Monitor de Ingressos Iniciado\\!*\n\n" +
                                "O monitoramento da loja do Uberlândia SAF está ativo\\.\n" +
                                $"⏱ Checagens a cada: `{_settings.IntervalSeconds}` segundos\\.\n" +
                                $"🔗 [Acessar Loja]({_settings.TargetUrl})";

        await _notificationService.SendAlertAsync(startupMessage, stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.IntervalSeconds));

        // Executa a primeira checagem imediatamente na inicialização
        await ExecutarChecagemAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ExecutarChecagemAsync(stoppingToken);
        }

        _logger.LogInformation("Serviço Monitor de Ingressos finalizado.");
    }

    private async Task ExecutarChecagemAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("Consultando {Url}...", _settings.TargetUrl);
            bool areTicketsAvailable = await _scraperService.HasTicketsAvailableAsync(ct);

            if (areTicketsAvailable && !_wereTicketsAvailable)
            {
                _logger.LogWarning("ALERTA: Novos ingressos detectados na loja!");

                string markdownMessage = "🚨 *NOVO INGRESSO DISPONÍVEL\\!*\n\n" +
                                         "Foram encontrados ingressos na loja do Uberlândia SAF\\.\n\n" +
                                         $"🔗 [Clique aqui para acessar a loja]({_settings.TargetUrl})";

                await _notificationService.SendAlertAsync(markdownMessage, ct);
                _wereTicketsAvailable = true;
            }
            else if (!areTicketsAvailable)
            {
                if (_wereTicketsAvailable)
                {
                    _logger.LogInformation("Os ingressos esgotaram novamente.");
                }

                _wereTicketsAvailable = false;
                _logger.LogInformation("Nenhum ingresso disponível no momento.");
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Falha de comunicação HTTP ao consultar a loja. Código: {StatusCode}", httpEx.StatusCode);
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogInformation("Checagem cancelada pelo encerramento do serviço.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao verificar ingressos.");
        }
    }
}
