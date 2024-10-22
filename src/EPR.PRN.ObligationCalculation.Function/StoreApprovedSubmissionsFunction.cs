using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EPR.PRN.ObligationCalculation.Function;

public class StoreApprovedSubmissionsFunction
{
    private readonly ISubmissionsDataService _submissionsService;
    private readonly IServiceBusProvider _serviceBusProvider;
    private readonly ILogger<StoreApprovedSubmissionsFunction> _logger;
    private readonly string _logPrefix;
    public StoreApprovedSubmissionsFunction(ILogger<StoreApprovedSubmissionsFunction> logger, ISubmissionsDataService submissionsService, IServiceBusProvider serviceBusProvider)
    {
        _logger = logger;
        _submissionsService = submissionsService;
        _serviceBusProvider = serviceBusProvider;
        _logPrefix = nameof(StoreApprovedSubmissionsFunction);
    }

    [Function("StoreApprovedSubmissionsFunction")]
    public async Task RunAsync([TimerTrigger("%StoreApprovedSubmissions:Schedule%")] TimerInfo myTimer)
    {
        try
        {
            _logger.LogInformation("[{LogPrefix}]: New session started", _logPrefix);

            var lastSuccessfulRunDate = "2024-01-01"; // Call endpoint to get last run date
            var approvedSubmissionEntities = await _submissionsService.GetApprovedSubmissionsData(lastSuccessfulRunDate);

            await _serviceBusProvider.SendApprovedSubmissionsToQueueAsync(approvedSubmissionEntities);
            
            // Call endpoint to update last run date
            
            _logger.LogInformation("[{LogPrefix}]: Completed storing submissions", _logPrefix);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{LogPrefix}]: Ended with error while storing approved submission", _logPrefix);
        }
    }
}