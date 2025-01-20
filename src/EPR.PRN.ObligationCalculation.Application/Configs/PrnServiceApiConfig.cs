using System.Diagnostics.CodeAnalysis;

namespace EPR.PRN.ObligationCalculation.Application.Configs;

[ExcludeFromCodeCoverage]
public class PrnServiceApiConfig
{
    public const string SectionName = "PrnServiceApi";
    public string BaseUrl { get; set; } = null!;
    public string PrnCalculateEndPoint { get; set; } = null!;
    public string LogPrefix { get; set; } = string.Empty;
	public string ClientId { get; set; } = string.Empty;
}
