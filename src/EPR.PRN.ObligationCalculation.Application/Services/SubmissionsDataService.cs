﻿using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;

namespace EPR.PRN.ObligationCalculation.Application.Services;

public class SubmissionsDataService : ISubmissionsDataService
{
    private readonly ILogger<SubmissionsDataService> _logger;
    private readonly HttpClient _httpClient;
    private readonly SubmissionsApiConfig _config;
    public SubmissionsDataService(ILogger<SubmissionsDataService> logger, HttpClient httpClient, IOptions<SubmissionsApiConfig> config)
    {
        _logger = logger;
        _httpClient = httpClient;
        _config = config.Value;
    }

    public async Task<List<ApprovedSubmissionEntity>> GetApprovedSubmissionsData(string approvedAfterDateString)
    {
        const string logMessageTemplate = ">>>>>>> Get Approved Submissions Data from {ApprovedAfterDateString} <<<<<<<<";
        _logger.LogInformation(logMessageTemplate, approvedAfterDateString);

        return await GetSubmissions(approvedAfterDateString);
    }

    private async Task<List<ApprovedSubmissionEntity>> GetSubmissions(string approvedAfterDateString)
    {
        string _submissionsBaseUrl = _config.BaseUrl;
        string _submissionsEndPoint = _config.EndPoint;

        string endpoint = _submissionsBaseUrl + _submissionsEndPoint + approvedAfterDateString;
        _logger.LogInformation("Fetching Submissions data from: {Endpoint}", endpoint);

        try
        {
            var result = await GetDataAsync(endpoint);

            if (string.IsNullOrEmpty(result))
            {
                _logger.LogWarning("No submissions data found for {ApprovedAfterDateString}", approvedAfterDateString);
                return new List<ApprovedSubmissionEntity>();
            }
            else
            {
                _logger.LogInformation("Submissions data: {Result}", result);

                return JsonConvert.DeserializeObject<List<ApprovedSubmissionEntity>>(result) ?? [];
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting submissions data from {Endpoint} : {Ex}", endpoint, ex.Message);
            throw;
        }
    }

    private async Task<string> GetDataAsync(string endpoint)
    {
        var response = await _httpClient.GetAsync(endpoint);

        return response.StatusCode.HasFlag(HttpStatusCode.OK) ? await response.Content.ReadAsStringAsync() : string.Empty;
    }
}