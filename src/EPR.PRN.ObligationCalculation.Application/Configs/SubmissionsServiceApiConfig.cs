using System.Diagnostics.CodeAnalysis;

namespace EPR.PRN.ObligationCalculation.Application.Configs;

[ExcludeFromCodeCoverage]
public class SubmissionsServiceApiConfig
{
    public const string SectionName = "SubmissionsServiceApi";
    public string BaseUrl { get; set; } = null!;
    public string SubmissionsEndPoint { get; set; } = null!;
    public string LogPrefix { get; set; } = string.Empty;
	public string ClientId { get; set; } = string.Empty;
	public int TimeoutFromSeconds { get; set; }
}
