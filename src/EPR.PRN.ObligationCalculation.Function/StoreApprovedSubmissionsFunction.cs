using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EPR.PRN.ObligationCalculation.Function
{
    public class StoreApprovedSubmissionsFunction
    {
        private readonly ILogger _logger;
        private readonly IAppInsightsProvider _appInsightsProvider;
        private static string LogPrefix = "[StoreApprovedSubmissionsFunction]:";
        private DateTime? CreatedDate = null;

        public StoreApprovedSubmissionsFunction(ILoggerFactory loggerFactory, IAppInsightsProvider appInsightsProvider)
        {
            _logger = loggerFactory.CreateLogger<StoreApprovedSubmissionsFunction>();
            _appInsightsProvider = appInsightsProvider;
        }

        [Function("StoreApprovedSubmissionsFunction")]
        //public async Task RunAsync([TimerTrigger("0 0 12 * * 1-5")] TimerInfo myTimer)
        public async Task RunAsync([TimerTrigger("*/10 * * * * *")] TimerInfo myTimer)
        {
            var justWork = await _appInsightsProvider.GetParameterForApprovedSubmissionsApiCall();
        }
    }
}
