using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace EPR.PRN.ObligationCalculation.Application.Services;

public class PrnService(ILogger<PrnService> logger, HttpClient httpClient, IOptions<PrnServiceApiConfig> config) : IPrnService
{
    public async Task ProcessApprovedSubmission(string submissions)
    {
        try
        {
            if (string.IsNullOrEmpty(submissions))
            {
                logger.LogInformation("{LogPrefix}: PrnService - ProcessApprovedSubmission - Submissions message is empty", config.Value.LogPrefix);
            }
            else
            {
                var submissionEntities = JsonConvert.DeserializeObject<List<ApprovedSubmissionEntity>>(submissions);
                if (submissionEntities != null)
                {
                    var submitterId = submissionEntities[0].SubmitterId;
                    string prnCalculateEndPoint = string.Format(config.Value.PrnCalculateEndPoint, submitterId);
                    logger.LogInformation("{LogPrefix}: PrnService - ProcessApprovedSubmission - Submissions request being sent to Endpoint: {Endpoint}, SubmitterId: {SubmitterId}, Entity Count: {Count} ", config.Value.LogPrefix, prnCalculateEndPoint, submitterId, submissionEntities.Count);

                    var response = await httpClient.PostAsJsonAsync(prnCalculateEndPoint, submissionEntities);
                    logger.LogInformation("{LogPrefix}: PrnService - ProcessApprovedSubmission - Calculate endpoint execution completed with status code - {StatusCode}", config.Value.LogPrefix, response.StatusCode);
                    
                    response.EnsureSuccessStatusCode();
                    logger.LogInformation("{LogPrefix}: PrnService - ProcessApprovedSubmission - Submissions message is posted to backend successfully", config.Value.LogPrefix);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{LogPrefix}: PrnService - ProcessApprovedSubmission - Error while submitting submissions data to endpoint {Endpoint}", config.Value.LogPrefix, config.Value.PrnCalculateEndPoint);
            throw;
        }
    }
}