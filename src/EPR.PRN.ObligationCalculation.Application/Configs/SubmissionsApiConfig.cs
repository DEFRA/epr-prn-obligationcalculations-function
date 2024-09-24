using System.Diagnostics.CodeAnalysis;

namespace EPR.PRN.ObligationCalculation.Application.Configs;

[ExcludeFromCodeCoverage]
public class SubmissionsApiConfig
{

    public const string SectionName = "Submissions";
    public string BaseUrl { get; set; } = null!;
    public string EndPoint { get; set; } = null!;
}
