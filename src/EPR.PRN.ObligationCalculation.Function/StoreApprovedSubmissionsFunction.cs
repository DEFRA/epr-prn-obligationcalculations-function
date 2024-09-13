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
        //private readonly ApiClient _apiClient;
        private readonly ISubmissionsDataService _submissionsService;
        private readonly IAppInsightsProvider _appInsightsProvider;

        private readonly string LogPrefix = "[StoreApprovedSubmissionsFunction]:";
        //private readonly string _submissionsBaseUrl;

        public StoreApprovedSubmissionsFunction(ILoggerFactory loggerFactory,
                                                ApiClient apiClient,
                                                IConfiguration configuration,
                                                ISubmissionsDataService submissionsService,
                                                IAppInsightsProvider appInsightsProvider)
        {
            _logger = loggerFactory.CreateLogger<StoreApprovedSubmissionsFunction>();
            //_apiClient = apiClient;
            _submissionsService = submissionsService;
            _appInsightsProvider = appInsightsProvider;
            //_submissionsBaseUrl = configuration["SubmissionsBaseUrl"] ?? throw new InvalidOperationException("SubmissionsBaseUrl configuration is missing.");
        }

        [Function("StoreApprovedSubmissionsFunction")]
        public async Task RunAsync([TimerTrigger("*/10 * * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation("{LogPrefix} >>>>> New session started <<<<< ", LogPrefix);

            DateTime createdDate = _appInsightsProvider.GetParameterForApprovedSubmissionsApiCall().Result;

            //await _submissionsService.GetApprovedSubmissionsData(createdDate.ToShortDateString());
        }
    }
}
