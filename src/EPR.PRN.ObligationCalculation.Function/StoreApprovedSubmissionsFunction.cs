using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EPR.PRN.ObligationCalculation.Function
{
    public class StoreApprovedSubmissionsFunction
    {
        private readonly ISubmissionsDataService _submissionsService;
        private readonly IAppInsightsProvider _appInsightsProvider;
        private readonly IServiceBusProvider _serviceBusProvider;
        private readonly ILogger<StoreApprovedSubmissionsFunction> _logger;

        private readonly string LogPrefix = "[StoreApprovedSubmissionsFunction]:";

        public StoreApprovedSubmissionsFunction(ILogger<StoreApprovedSubmissionsFunction> logger, IConfiguration configuration, ISubmissionsDataService submissionsService, IAppInsightsProvider appInsightsProvider, IServiceBusProvider serviceBusProvider)
        {
            _logger = logger;
            _submissionsService = submissionsService;
            _appInsightsProvider = appInsightsProvider;
            _serviceBusProvider = serviceBusProvider;
        }

        [Function("StoreApprovedSubmissionsFunction")]
        public async Task RunAsync([TimerTrigger("*/10 * * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation("{LogPrefix} >>>>> New session started <<<<< ", LogPrefix);

            DateTime createdDate = _appInsightsProvider.GetParameterForApprovedSubmissionsApiCall().Result;
            var approvedSubmissionEntities = await _submissionsService.GetApprovedSubmissionsData(createdDate.ToString("yyyy-MM-dd"));

            if (approvedSubmissionEntities.Count > 0)
            {
                _logger.LogInformation("{LogPrefix} >>>>> Number of Submissions received : {ApprovedSubmissionEntitiesCount}", LogPrefix, approvedSubmissionEntities.Count);
                await _serviceBusProvider.SendApprovedSubmissionsToQueue(approvedSubmissionEntities);
            }
            else
            {
                _logger.LogInformation("{LogPrefix} >>>>> No new submissions received <<<<< ", LogPrefix);
            }
        }
    }
}
