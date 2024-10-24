﻿using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Json;

namespace EPR.PRN.ObligationCalculation.Application.Services;

public class PrnService : IPrnService
{
    private readonly ILogger<PrnService> _logger;
    private readonly HttpClient _httpClient;
    private readonly CommonBackendApiConfig _config;
    private readonly string _logPrefix;

    public PrnService(ILogger<PrnService> logger, HttpClient httpClient, IOptions<CommonBackendApiConfig> config)
    {
        _logger = logger;
        _httpClient = httpClient;
        _config = config.Value;
        _logPrefix = nameof(PrnService);
    }

    public async Task<string> GetLastSuccessfulRunDate()
    {
        var response = await _httpClient.GetAsync(_config.LastSuccessfulRunDateEndPoint);
        return response.StatusCode.HasFlag(HttpStatusCode.OK)
            ? await response.Content.ReadAsStringAsync()
            : string.Empty;
    }

    public async Task UpdateLastSuccessfulRunDate(DateTime currentDateTime)
    {
        string requestUri = $"{_config.LastSuccessfulRunDateEndPoint}/{currentDateTime}";
        await _httpClient.PutAsync(requestUri, null);
    }

    public async Task ProcessApprovedSubmission(string submissions)
    {
        if (string.IsNullOrEmpty(submissions))
        {
            _logger.LogInformation("[{LogPrefix}]: Submissions message is empty", _logPrefix);
        }
        else
        {
            var submissionEntities = JsonConvert.DeserializeObject<List<ApprovedSubmissionEntity>>(submissions);
            if (submissionEntities != null)
            {
                var organisationId = Guid.NewGuid();
                string prnCalculateEndPoint = string.Format(_config.PrnCalculateEndPoint, organisationId);
                var response = await _httpClient.PostAsJsonAsync(prnCalculateEndPoint, submissions);
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("[{LogPrefix}]: Submissions message is posted to backend", _logPrefix);
            }
        }
    }
}