using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EPR.PRN.ObligationCalculation.Function
{
    public class StoreApprovedSubmissionsFunction
    {
        private readonly ISubmissionsDataService _submissionsService;
        private readonly IServiceBusProvider _serviceBusProvider;
        private readonly ILogger<StoreApprovedSubmissionsFunction> _logger;

        private readonly string LogPrefix = ApplicationConstants.StoreApprovedSubmissionsFunctionLogPrefix;

        public StoreApprovedSubmissionsFunction(ILogger<StoreApprovedSubmissionsFunction> logger, ISubmissionsDataService submissionsService, IServiceBusProvider serviceBusProvider)
        {
            _logger = logger;
            _submissionsService = submissionsService;
            _serviceBusProvider = serviceBusProvider;
        }

        [Function("StoreApprovedSubmissionsFunction")]
        public async Task RunAsync([TimerTrigger("%StoreApprovedSubmissions:Schedule%")] TimerInfo myTimer)
        {
            _logger.LogInformation("{LogPrefix} >>>>> New session started <<<<< ", LogPrefix);

            var lastSuccessfulRunDate = "2024-01-01"; // Get using lastrun GET endpoint
            var approvedSubmissionEntities = await _submissionsService.GetApprovedSubmissionsData(lastSuccessfulRunDate);

            if (approvedSubmissionEntities.Count > 0)
            {
                _logger.LogInformation("{LogPrefix} >>>>> Number of Submissions received : {ApprovedSubmissionEntitiesCount}", LogPrefix, approvedSubmissionEntities.Count);
                await _serviceBusProvider.SendApprovedSubmissionsToQueue(approvedSubmissionEntities);
                _logger.LogInformation("{LogPrefix} COMPLETED >>>>> Number of Submissions send to queue : {ApprovedSubmissionEntitiesCount}", LogPrefix, approvedSubmissionEntities.Count);
            }
            else
            {
                _logger.LogInformation("{LogPrefix} COMPLETED >>>>> No new submissions received and send to queue <<<<< ", LogPrefix);
            }
            // Update lastrun date using PUT endpoint
        }
    }
}
