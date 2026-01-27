using System.Diagnostics.CodeAnalysis;
using EPR.PRN.ObligationCalculation.Application.Configs;
using Microsoft.Extensions.Options;

namespace EPR.PRN.ObligationCalculation.Function.Handlers;

[ExcludeFromCodeCoverage]
public class EprPrnCommonBackendAuthorisationHandler(IOptions<PrnServiceApiConfig> config) : BaseServiceAuthorisationHandler(config.Value.ClientId)
{
}