using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Function.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Text.Json;

namespace EPR.PRN.ObligationCalculation.Function;

public class StoreApprovedSubmissionsFunction(ILogger<StoreApprovedSubmissionsFunction> logger, IEprCommonDataApiService eprCommonDataApiService, IEprPrnCommonBackendService eprPrnCommonBackendService, IOptions<ApplicationConfig> config)
{
    private static readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };

    [Function("StoreApprovedSubmissionsFunction")]
    public async Task PublishAsync([TimerTrigger("%StoreApprovedSubmissions:Schedule%")] TimerInfo myTimer)
    {
        logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction - New session started", config.Value.LogPrefix);

        if (!config.Value.FunctionIsEnabled)
        {
            logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction - Exiting function as FunctionIsEnabled is set to {config.Value.FunctionIsEnabled}", config.Value.LogPrefix, config.Value.FunctionIsEnabled);
            return;
        }

        try
        {
            var approvedSubmissionEntities = await eprCommonDataApiService.GetApprovedSubmissionsData(DateTime.Now.Date.ToString("yyyy-MM-dd"));
            logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction - Sending Approved submission entities", config.Value.LogPrefix);

            if (approvedSubmissionEntities.Count == 0)
            {
                logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction - No submissions received", config.Value.LogPrefix);
                return;
            }

            var groupedSubmissions = approvedSubmissionEntities
                .GroupBy(s => s.SubmitterId)
                .Select(g => (SubmitterId: g.Key, Submissions: g.ToList()));

            foreach (var (submitterId, submissions) in groupedSubmissions)
            {
                try
                {
                    logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction - calculating submitterId {SubmitterId}", config.Value.LogPrefix, submitterId);
                    await eprPrnCommonBackendService.CalculateApprovedSubmission(JsonSerializer.Serialize(submissions, jsonOptions));
                    logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction - successfully calculated submitterId {MessageId}", config.Value.LogPrefix, submitterId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{LogPrefix}: ProcessApprovedSubmissionsFunction: Exception occurred while sending processing message: {Message}", config.Value.LogPrefix, ex.Message);
                }
            }
            logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: Completed storing submissions", config.Value.LogPrefix);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{LogPrefix}: StoreApprovedSubmissionsFunction: Ended with error while storing approved submission", config.Value.LogPrefix);
            throw;
        }
    }
}