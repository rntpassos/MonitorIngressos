using MonitorIngressos;
using MonitorIngressos.Configuration;
using MonitorIngressos.Services;

var builder = Host.CreateApplicationBuilder(args);

// Registro das seções de configuração
builder.Services.Configure<MonitorSettings>(
    builder.Configuration.GetSection(MonitorSettings.SectionName));
builder.Services.Configure<TelegramSettings>(
    builder.Configuration.GetSection(TelegramSettings.SectionName));

// Configuração do HttpClient nomeado com User-Agent padrão
var monitorSettings = builder.Configuration
    .GetSection(MonitorSettings.SectionName)
    .Get<MonitorSettings>() ?? new MonitorSettings();

builder.Services.AddHttpClient("MonitorClient", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        string.IsNullOrWhiteSpace(monitorSettings.UserAgent)
            ? "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
            : monitorSettings.UserAgent);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Registro dos serviços na DI
builder.Services.AddSingleton<INotificationService, TelegramNotificationService>();
builder.Services.AddTransient<IScraperService, ScraperService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
