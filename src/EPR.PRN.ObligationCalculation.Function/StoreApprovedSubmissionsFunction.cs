using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EPR.PRN.ObligationCalculation.Function;

public class StoreApprovedSubmissionsFunction(ILogger<StoreApprovedSubmissionsFunction> logger, ISubmissionsDataService submissionsService, IServiceBusProvider serviceBusProvider, IOptions<ApplicationConfig> config)
{
    [Function(Functions.StoreApprovedSubmissionsFunction)]
    public async Task RunAsync([TimerTrigger("%StoreApprovedSubmissions:Schedule%")] TimerInfo myTimer)
    {
        logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: New session started", config.Value.LogPrefix);

        if (!config.Value.FunctionIsEnabled)
        {
            logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: Exiting function as FunctionIsEnabled is set to {config.Value.FunctionIsEnabled}", config.Value.LogPrefix, config.Value.FunctionIsEnabled);
            return;
        }

        try
        {
            var approvedSubmissionEntities = await submissionsService.GetApprovedSubmissionsData(DateTime.Now.Date.ToString("yyyy-MM-dd"));
            logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: Approved submission entities retrieved from backend", config.Value.LogPrefix);

            await serviceBusProvider.SendApprovedSubmissionsToQueueAsync(approvedSubmissionEntities);
            logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: Sent approved submission entities to queue...", config.Value.LogPrefix);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{LogPrefix}: StoreApprovedSubmissionsFunction: Ended with error while storing approved submission", config.Value.LogPrefix);
            throw;
        }
    }
}