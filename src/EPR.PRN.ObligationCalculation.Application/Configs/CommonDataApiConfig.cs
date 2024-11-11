using System.Diagnostics.CodeAnalysis;

namespace EPR.PRN.ObligationCalculation.Application.Configs;

[ExcludeFromCodeCoverage]
public class CommonDataApiConfig
{
    public const string SectionName = "CommonDataApi";
    public string BaseUrl { get; set; } = null!;
    public string SubmissionsEndPoint { get; set; } = null!;
    public string LogPrefix { get; set; } = string.Empty;
}
