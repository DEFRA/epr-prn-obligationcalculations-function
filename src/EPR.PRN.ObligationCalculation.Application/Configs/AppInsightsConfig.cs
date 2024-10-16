using System.Diagnostics.CodeAnalysis;

namespace EPR.PRN.ObligationCalculation.Application.Configs;

[ExcludeFromCodeCoverage]
public class AppInsightsConfig
{
    public const string SectionName = "AppInsights";
    public string ClientId { get; set; } = null!;
    public string TenantId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
    public string WorkspaceId { get; set; } = null!;
}
