using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EPR.PRN.ObligationCalculation.Function
{
    public class StoreApprovedSubmissionsFunction
    {
        private readonly ILogger _logger;
        private static string LogPrefix = "[StoreApprovedSubmissionsFunction]:";
        private DateTime? CreatedDate = null;

        public StoreApprovedSubmissionsFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<StoreApprovedSubmissionsFunction>();
        }

        [Function("Function1")]
        public void Run([TimerTrigger("0 0 12 * * 1-5")] TimerInfo myTimer)
        {
            _logger.LogInformation($"{LogPrefix} New session started");

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }

        public static DateTime GetCreatedDateFromLogs()
        {
            return DateTime.UtcNow;
        }
    }
}
