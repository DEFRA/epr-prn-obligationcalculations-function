using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EPR.PRN.ObligationCalculation.Function;

public class StoreApprovedSubmissionsFunction
{
    private readonly ISubmissionsDataService _submissionsService;
    private readonly IServiceBusProvider _serviceBusProvider;
    private readonly ILogger<StoreApprovedSubmissionsFunction> _logger;
    private readonly ApplicationConfig _config;
    private readonly string _logPrefix;
    public StoreApprovedSubmissionsFunction(ILogger<StoreApprovedSubmissionsFunction> logger, ISubmissionsDataService submissionsService, IServiceBusProvider serviceBusProvider, IOptions<ApplicationConfig> config)
    {
        _logger = logger;
        _submissionsService = submissionsService;
        _serviceBusProvider = serviceBusProvider;
        _config = config.Value;
        _logPrefix = nameof(StoreApprovedSubmissionsFunction);
    }

    [Function("StoreApprovedSubmissionsFunction")]
    public async Task RunAsync([TimerTrigger("%StoreApprovedSubmissions:Schedule%")] TimerInfo myTimer)
    {
        try
        {
            _logger.LogInformation("[{LogPrefix}]: New session started", _logPrefix);

            var lastSuccessfulRunDate = await _serviceBusProvider.GetLastSuccessfulRunDateFromQueue();
            if (string.IsNullOrEmpty(lastSuccessfulRunDate))
            {
                lastSuccessfulRunDate = _config.DefaultRunDate;
                _logger.LogInformation("[{LogPrefix}]: Last run date {Date} used from configuration values", _logPrefix, lastSuccessfulRunDate);
            }

            var approvedSubmissionEntities = await _submissionsService.GetApprovedSubmissionsData(lastSuccessfulRunDate);
            await _serviceBusProvider.SendApprovedSubmissionsToQueueAsync(approvedSubmissionEntities);
            
            var currectRunDate = DateTime.Now.Date.ToString("yyyy-MM-dd");
            await _serviceBusProvider.SendSuccessfulRunDateToQueue(currectRunDate);

            _logger.LogInformation("[{LogPrefix}]: Completed storing submissions", _logPrefix);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{LogPrefix}]: Ended with error while storing approved submission", _logPrefix);
        }
    }
}