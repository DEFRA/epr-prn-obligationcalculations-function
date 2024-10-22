using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EPR.PRN.ObligationCalculation.Function;

public class ProcessApprovedSubmissionsFunction
{
    private readonly IServiceBusProvider _serviceBusProvider;
    private readonly ILogger<ProcessApprovedSubmissionsFunction> _logger;
    private readonly string _logPrefix;

    public ProcessApprovedSubmissionsFunction(ILogger<ProcessApprovedSubmissionsFunction> logger, IServiceBusProvider serviceBusProvider)
    {
        _logger = logger;
        _serviceBusProvider = serviceBusProvider;
        _logPrefix = nameof(ProcessApprovedSubmissionsFunction);
    }

    [Function("ProcessApprovedSubmissionsFunction")]
    public async Task RunAsync([TimerTrigger("%ProcessApprovedSubmissions:Schedule%")] TimerInfo myTimer)
    {
        try
        {
            _logger.LogInformation("[{LogPrefix}]: New session started", _logPrefix);
            await _serviceBusProvider.ReceiveAndProcessMessagesFromQueueAsync();
            _logger.LogInformation("[{LogPrefix}]: Completed processing submissions", _logPrefix);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "[{LogPrefix}]: Error while processing approved submission", _logPrefix);
        }
    }
}