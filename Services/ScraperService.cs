using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitorIngressos.Configuration;

namespace MonitorIngressos.Services;

public class ScraperService : IScraperService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MonitorSettings _settings;
    private readonly ILogger<ScraperService> _logger;

    public ScraperService(
        IHttpClientFactory httpClientFactory,
        IOptions<MonitorSettings> settings,
        ILogger<ScraperService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> HasTicketsAvailableAsync(CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("MonitorClient");

        HttpResponseMessage response = await client.GetAsync(_settings.TargetUrl, ct);

        // A loja oficial do Uberlândia SAF retorna HTTP 404 quando a categoria não possui itens cadastrados,
        // mantendo contudo a renderização completa do DOM com <ul class="categories-content"><li class="empty_base">...
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }

        string htmlContent = await response.Content.ReadAsStringAsync(ct);

        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        // Seletor que indica explicitamente que não há ingressos cadastrados
        var emptyElement = doc.DocumentNode.SelectSingleNode("//li[contains(@class, 'empty_base')]");
        if (emptyElement != null)
        {
            return false;
        }

        // Validação complementar: checar se a lista pai 'categories-content' existe
        var listElement = doc.DocumentNode.SelectSingleNode("//ul[contains(@class, 'categories-content')]");
        if (listElement != null)
        {
            _logger.LogInformation("Detecção positiva: lista 'categories-content' ativa sem nó 'empty_base'.");
            return true;
        }

        // Se for um 404 genérico sem a estrutura da loja
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Página retornou 404 sem a estrutura 'categories-content' esperada.");
            return false;
        }

        return true;
    }
}
