using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitorIngressos.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace MonitorIngressos.Services;

public class TelegramNotificationService : INotificationService
{
    private readonly ITelegramBotClient? _botClient;
    private readonly TelegramSettings _settings;
    private readonly ILogger<TelegramNotificationService> _logger;

    public TelegramNotificationService(
        IOptions<TelegramSettings> settings,
        ILogger<TelegramNotificationService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_settings.BotToken) || _settings.BotToken.Contains("SUBSTITUIR"))
        {
            _logger.LogWarning("BotToken do Telegram não está configurado ou contém o valor padrão no appsettings.json. As notificações pelo Telegram estarão desativadas até a configuração de um token válido do BotFather.");
            return;
        }

        try
        {
            _botClient = new TelegramBotClient(_settings.BotToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inicializar TelegramBotClient com o token fornecido.");
        }
    }

    public async Task SendAlertAsync(string message, CancellationToken ct = default)
    {
        try
        {
            if (_botClient == null)
            {
                _logger.LogWarning("Notificação não enviada: TelegramBotClient não inicializado. Configure o BotToken no appsettings.json.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_settings.ChatId) || _settings.ChatId.Contains("SUBSTITUIR"))
            {
                _logger.LogError("ChatId do Telegram não configurado no appsettings.json.");
                return;
            }

            await _botClient.SendMessage(
                chatId: _settings.ChatId,
                text: message,
                parseMode: ParseMode.MarkdownV2,
                cancellationToken: ct);

            _logger.LogInformation("Notificação enviada com sucesso para o Telegram.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar notificação para o bot do Telegram.");
        }
    }
}
