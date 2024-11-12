using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace EPR.PRN.ObligationCalculation.Function;

public class StoreApprovedSubmissionsFunction(ILogger<StoreApprovedSubmissionsFunction> logger, ISubmissionsDataService submissionsService, IServiceBusProvider serviceBusProvider, IOptions<ApplicationConfig> config)
{
    [Function("StoreApprovedSubmissionsFunction")]
    public async Task RunAsync([TimerTrigger("%StoreApprovedSubmissions:Schedule%")] TimerInfo myTimer)
    {
        try
        {
            logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: New session started", config.Value.LogPrefix);

            var lastSuccessfulRunDate = string.Empty;
            if (config.Value.UseDefaultRunDate)
            {
                lastSuccessfulRunDate = config.Value.DefaultRunDate;
                logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: Last run date {Date} used from configuration values", config.Value.LogPrefix, lastSuccessfulRunDate);
            }
            else
            {
                lastSuccessfulRunDate = await serviceBusProvider.GetLastSuccessfulRunDateFromQueue();
                logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: Last run date {Date} retrieved from queue", config.Value.LogPrefix, lastSuccessfulRunDate);
            }

            if (string.IsNullOrEmpty(lastSuccessfulRunDate))
            {
                logger.LogError("{LogPrefix}: StoreApprovedSubmissionsFunction: Last succesful run date is empty and function is terminated", config.Value.LogPrefix);
                return;
            }

            var approvedSubmissionEntities = await submissionsService.GetApprovedSubmissionsData(lastSuccessfulRunDate);
            logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: Approved submission entities retrieved from backnend {ApprovedSubmissionEntities}", config.Value.LogPrefix, JsonConvert.SerializeObject(approvedSubmissionEntities));

            logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: Sending Approved submission entities to queue...", config.Value.LogPrefix);
            await serviceBusProvider.SendApprovedSubmissionsToQueueAsync(approvedSubmissionEntities);

            var currectRunDate = DateTime.Now.Date.ToString("yyyy-MM-dd");
            logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: Adding Successful RunDate To Queue {CurrectRunDate} ...", config.Value.LogPrefix, currectRunDate.ToString());
            await serviceBusProvider.SendSuccessfulRunDateToQueue(currectRunDate);

            logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: Completed storing submissions", config.Value.LogPrefix);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{LogPrefix}: StoreApprovedSubmissionsFunction: Ended with error while storing approved submission", config.Value.LogPrefix);
            throw;
        }
    }
}