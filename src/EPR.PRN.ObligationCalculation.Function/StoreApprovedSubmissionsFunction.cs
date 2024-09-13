using EPR.PRN.ObligationCalculation.Application;
using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EPR.PRN.ObligationCalculation.Function
{
    public class StoreApprovedSubmissionsFunction
    {
        private readonly ILogger _logger;
        private readonly ISubmissionsDataService _submissionsService;
        private readonly IAppInsightsProvider _appInsightsProvider;

        private readonly string LogPrefix = "[StoreApprovedSubmissionsFunction]:";

        public StoreApprovedSubmissionsFunction(ILoggerFactory loggerFactory, IConfiguration configuration, ISubmissionsDataService submissionsService, IAppInsightsProvider appInsightsProvider)
        {
            _logger = loggerFactory.CreateLogger<StoreApprovedSubmissionsFunction>();
            _submissionsService = submissionsService;
            _appInsightsProvider = appInsightsProvider;
        }

        [Function("StoreApprovedSubmissionsFunction")]
        public async Task RunAsync([TimerTrigger("*/10 * * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation("{LogPrefix} >>>>> New session started <<<<< ", LogPrefix);

            DateTime createdDate = _appInsightsProvider.GetParameterForApprovedSubmissionsApiCall().Result;
            await _submissionsService.GetApprovedSubmissionsData(createdDate.ToString("yyyy-MM-dd"));
        }
    }
}
