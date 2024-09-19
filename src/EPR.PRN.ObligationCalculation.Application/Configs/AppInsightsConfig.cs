using System.Diagnostics.CodeAnalysis;

namespace EPR.PRN.ObligationCalculation.Application.Configs;

[ExcludeFromCodeCoverage]
public class AppInsightsConfig
{
    public const string SectionName = "AppInsights";
    public string AppId { get; set; } = null!;
    public string ApiKey { get; set; } = null!;
    public string ApiUrl { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string TenantId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
    public string WorkspaceId { get; set; } = null!;
}
