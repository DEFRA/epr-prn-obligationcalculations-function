using System.Diagnostics.CodeAnalysis;

namespace EPR.PRN.ObligationCalculation.Application.Configs;

[ExcludeFromCodeCoverage]
public class ApplicationConfig
{

    public const string SectionName = "AppConfig";
    public bool DeveloperMode { get; set; }
    public string DefaultRunDate { get; set; } = null!;
}
