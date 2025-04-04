﻿using System.Diagnostics.CodeAnalysis;

namespace EPR.PRN.ObligationCalculation.Application.Configs;

[ExcludeFromCodeCoverage]
public class ServiceBusConfig
{
    public const string SectionName = "ServiceBus";
    public string FullyQualifiedNamespace { get; set; } = null!;
    public string ObligationQueueName { get; set; } = null!;
    public string ObligationLastSuccessfulRunQueueName { get; set; } = null!;
    public string ConnectionString { get; set; } = null!;
    public string LogPrefix { get; set; } = string.Empty;
}
