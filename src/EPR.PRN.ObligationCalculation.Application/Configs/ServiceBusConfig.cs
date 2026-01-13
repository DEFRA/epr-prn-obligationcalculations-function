using System.Diagnostics.CodeAnalysis;

namespace EPR.PRN.ObligationCalculation.Application.Configs;

[ExcludeFromCodeCoverage]
public class ServiceBusConfig
{
    public const string SectionName = "ServiceBus";
    public string? FullyQualifiedNamespace { get; set; }
    public string? ObligationQueueName { get; set; }
    public string? ObligationLastSuccessfulRunQueueName { get; set; }
    public string LogPrefix { get; set; } = string.Empty;
}
