using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace EPR.PRN.ObligationCalculation.Function;

public class StoreApprovedSubmissionsFunction
{
    private readonly ISubmissionsDataService _submissionsService;
    private readonly IServiceBusProvider _serviceBusProvider;
    private readonly ILogger<StoreApprovedSubmissionsFunction> _logger;
    private readonly ApplicationConfig _config;

    public StoreApprovedSubmissionsFunction(ILogger<StoreApprovedSubmissionsFunction> logger, ISubmissionsDataService submissionsService, IServiceBusProvider serviceBusProvider, IOptions<ApplicationConfig> config)
    {
        _logger = logger;
        _submissionsService = submissionsService;
        _serviceBusProvider = serviceBusProvider;
        _config = config.Value;
    }

    [Function("StoreApprovedSubmissionsFunction")]
    public async Task RunAsync([TimerTrigger("%StoreApprovedSubmissions:Schedule%")] TimerInfo myTimer)
    {
        try
        {
            _logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: New session started", _config.LogPrefix);

            var lastSuccessfulRunDate = string.Empty;
            if (_config.UseDefaultRunDate)
            {
                lastSuccessfulRunDate = _config.DefaultRunDate;
                _logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: Last run date {Date} used from configuration values", _config.LogPrefix, lastSuccessfulRunDate);
            }
            else
            {
                lastSuccessfulRunDate = await _serviceBusProvider.GetLastSuccessfulRunDateFromQueue();
                _logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: Last run date {Date} retrieved from queue", _config.LogPrefix, lastSuccessfulRunDate);
            }

            if (string.IsNullOrEmpty(lastSuccessfulRunDate))
            {
                _logger.LogError("{LogPrefix}: StoreApprovedSubmissionsFunction: Last succesful run date is empty and function is terminated", _config.LogPrefix);
                return;
            }

            var approvedSubmissionEntities = await _submissionsService.GetApprovedSubmissionsData(lastSuccessfulRunDate);
            _logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: Approved submission entities retrieved from backnend {ApprovedSubmissionEntities}", _config.LogPrefix, JsonConvert.SerializeObject(approvedSubmissionEntities));

            _logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: Sending Approved submission entities to queue...", _config.LogPrefix);
            await _serviceBusProvider.SendApprovedSubmissionsToQueueAsync(approvedSubmissionEntities);

            var currectRunDate = DateTime.Now.Date.ToString("yyyy-MM-dd");
            _logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: Adding Successful RunDate To Queue {CurrectRunDate} ...", _config.LogPrefix, currectRunDate.ToString());
            await _serviceBusProvider.SendSuccessfulRunDateToQueue(currectRunDate);

            _logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: Completed storing submissions", _config.LogPrefix);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{LogPrefix}: StoreApprovedSubmissionsFunction: Ended with error while storing approved submission", _config.LogPrefix);
            throw;
        }
    }
}