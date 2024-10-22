using System.Diagnostics.CodeAnalysis;

namespace EPR.PRN.ObligationCalculation.Application.Configs;

[ExcludeFromCodeCoverage]
public class CommonBackendApiConfig
{

    public const string SectionName = "CommonBackendApi";
    public string BaseUrl { get; set; } = null!;
    public string PrnCalculateEndPoint { get; set; } = null!;
    public string LastSuccessfulRunDateEndPoint { get; set; } = null!;
}
