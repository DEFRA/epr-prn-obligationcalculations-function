using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using EPR.PRN.ObligationCalculation.Function.Extensions;
using EPR.PRN.ObligationCalculation.Function.Handlers;
using EPR.PRN.ObligationCalculation.Function.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace EPR.PRN.ObligationCalculation.Function;

public class ObligationSyncFunction(ILogger<ObligationSyncFunction> logger, IEprCommonDataApiService eprCommonDataApiService, IEprPrnCommonBackendService eprPrnCommonBackendService, IServiceBusProvider serviceBusProvider, IOptions<ApplicationConfig> config)
{
    private static readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };

    [Function("StoreApprovedSubmissionsFunction")]
    public async Task PublishAsync([TimerTrigger("%StoreApprovedSubmissions:Schedule%")] TimerInfo myTimer)
    {
        logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: New session started", config.Value.LogPrefix);

        if (!config.Value.FunctionIsEnabled)
        {
            logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: Exiting function as FunctionIsEnabled is set to {config.Value.FunctionIsEnabled}", config.Value.LogPrefix, config.Value.FunctionIsEnabled);
            return;
        }

        try
        {
            var lastSuccessfulRunDateFromQueue = await serviceBusProvider.GetLastSuccessfulRunDateFromQueue();
            logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: Last run date {Date} retrieved from queue", config.Value.LogPrefix, lastSuccessfulRunDateFromQueue);

            var lastSuccessfulRunDate = string.IsNullOrEmpty(lastSuccessfulRunDateFromQueue) ? config.Value.DefaultRunDate : lastSuccessfulRunDateFromQueue;

            if (string.IsNullOrEmpty(lastSuccessfulRunDate))
            {
                logger.LogError("{LogPrefix}: StoreApprovedSubmissionsFunction: Last succesful run date is empty and function is terminated", config.Value.LogPrefix);
                return;
            }

            var approvedSubmissionEntities = await eprCommonDataApiService.GetApprovedSubmissionsData(lastSuccessfulRunDate);
            logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: Sending Approved submission entities to queue...", config.Value.LogPrefix);

            if (approvedSubmissionEntities.Count == 0)
            {
                logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction - No new submissions received from pom endpoint to queue", config.Value.LogPrefix);
                return;
            }

            var groupedSubmissions = approvedSubmissionEntities
                .GroupBy(s => s.SubmitterId)
                .Select(g => (SubmitterId: g.Key, Submissions: g.ToList()));

            foreach (var (submitterId, submissions) in groupedSubmissions)
            {
                await serviceBusProvider.SendApprovedSubmissionsToQueueAsync(submitterId, submissions);
            }

            logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction - Messages have been published to the obligation queue.", config.Value.LogPrefix);
            await serviceBusProvider.SendSuccessfulRunDateToQueue(DateTime.Now.Date.ToString("yyyy-MM-dd"));
            logger.LogInformation("{LogPrefix}: StoreApprovedSubmissionsFunction: Completed storing submissions", config.Value.LogPrefix);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{LogPrefix}: StoreApprovedSubmissionsFunction: Ended with error while storing approved submission", config.Value.LogPrefix);
            throw;
        }
    }

    [Function("ProcessApprovedSubmissionsFunction")]
    public async Task ProcessAsync([ServiceBusTrigger("%ServiceBus:ObligationQueueName%", Connection = "ServiceBus")] ServiceBusReceivedMessage message)
    {
        if (!config.Value.FunctionIsEnabled)
        {
            logger.LogInformation("{LogPrefix}: ProcessApprovedSubmissionsFunction: Exiting function as FunctionIsEnabled is set to {FunctionIsEnabled}", config.Value.LogPrefix, config.Value.FunctionIsEnabled);
            return;
        }

        try
        {
            logger.LogInformation("{LogPrefix}: ProcessApprovedSubmissionsFunction: Received message with ID: {MessageId}", config.Value.LogPrefix, message.MessageId);
            await eprPrnCommonBackendService.CalculateApprovedSubmission(message.Body.ToString());
            logger.LogInformation("{LogPrefix}: ProcessApprovedSubmissionsFunction: Processed message with ID: {MessageId}", config.Value.LogPrefix, message.MessageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{LogPrefix}: ProcessApprovedSubmissionsFunction: Exception occurred while processing message: {Message}", config.Value.LogPrefix, ex.Message);
            throw;
        }
    }
}