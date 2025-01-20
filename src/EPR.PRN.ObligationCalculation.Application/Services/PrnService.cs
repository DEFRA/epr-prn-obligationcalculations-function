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
                    var organisationId = submissionEntities[0].OrganisationId;
                    string prnCalculateEndPoint = string.Format(config.Value.PrnCalculateEndPoint, organisationId);
                    logger.LogInformation("{LogPrefix}: PrnService - ProcessApprovedSubmission - Submissions request being sent to Endpoint : {Endpoint}, Submissions : {Submissions}", config.Value.LogPrefix, prnCalculateEndPoint, submissions);

                    var response = await httpClient.PostAsJsonAsync(prnCalculateEndPoint, submissionEntities);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    logger.LogInformation("{LogPrefix}: PrnService - ProcessApprovedSubmission - Submissions response received : {Response}", config.Value.LogPrefix, responseContent);
                    
                    response.EnsureSuccessStatusCode();
                    logger.LogInformation("{LogPrefix}: PrnService - ProcessApprovedSubmission - Submissions message is posted to backend", config.Value.LogPrefix);
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