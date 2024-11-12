using System.Diagnostics.CodeAnalysis;

namespace EPR.PRN.ObligationCalculation.Application.Configs;

[ExcludeFromCodeCoverage]
public class ApplicationConfig
{
    public const string SectionName = "ApplicationConfig";
    public bool DeveloperMode { get; set; }
    public bool UseDefaultRunDate { get; set; }
    public string DefaultRunDate { get; set; } = null!;
    public string LogPrefix { get; set; } = string.Empty;
}
