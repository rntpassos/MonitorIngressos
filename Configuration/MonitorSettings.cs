namespace MonitorIngressos.Configuration;

public class MonitorSettings
{
    public const string SectionName = "MonitorSettings";

    public string TargetUrl { get; set; } = "https://loja.uberlandiasaf.com.br/categoria/ingressos";
    public int IntervalSeconds { get; set; } = 120;
    public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";
}
