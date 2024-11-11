using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace EPR.PRN.ObligationCalculation.Application.Services;

public class PrnService : IPrnService
{
    private readonly ILogger<PrnService> _logger;
    private readonly HttpClient _httpClient;
    private readonly CommonBackendApiConfig _config;

    public PrnService(ILogger<PrnService> logger, HttpClient httpClient, IOptions<CommonBackendApiConfig> config)
    {
        _logger = logger;
        _httpClient = httpClient;
        _config = config.Value;
    }

    public async Task ProcessApprovedSubmission(string submissions)
    {
        try
        {
            if (string.IsNullOrEmpty(submissions))
            {
                _logger.LogInformation("[{LogPrefix}]: PrnService - Submissions message is empty", _config.LogPrefix);
            }
            else
            {
                var submissionEntities = JsonConvert.DeserializeObject<List<ApprovedSubmissionEntity>>(submissions);
                if (submissionEntities != null)
                {
                    var organisationId = submissionEntities[0].OrganisationId;
                    string prnCalculateEndPoint = string.Format(_config.PrnCalculateEndPoint, organisationId);
                    _logger.LogInformation("[{LogPrefix}]: PrnService - Submissions request being sent to Endpoint : {Endpoint}, Submissions : {Submissions}", _config.LogPrefix, prnCalculateEndPoint, submissions);

                    var response = await _httpClient.PostAsJsonAsync(prnCalculateEndPoint, submissions);
                    _logger.LogInformation("[{LogPrefix}]: PrnService - Submissions response received : {Response}", _config.LogPrefix, JsonConvert.SerializeObject(response));
                    
                    response.EnsureSuccessStatusCode();
                    _logger.LogInformation("[{LogPrefix}]: PrnService - Submissions message is posted to backend", _config.LogPrefix);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{LogPrefix}]: PrnService - Error while submitting submissions data to endpoint {Endpoint}", _config.LogPrefix, _config.PrnCalculateEndPoint);
            throw;
        }
    }
}