using System.Text.Json;
using Azure.Messaging.ServiceBus;
using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EPR.PRN.ObligationCalculation.Application.Services;

public class ServiceBusProvider(ILogger<ServiceBusProvider> logger, ServiceBusClient serviceBusClient, IOptions<ServiceBusConfig> config) : IServiceBusProvider
{
    private static readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };
    
    public async Task SendApprovedSubmissionsToQueueAsync(List<ApprovedSubmissionEntity> approvedSubmissionEntities)
    {
        try
        {
            if (approvedSubmissionEntities.Count == 0)
            {
                logger.LogInformation("{LogPrefix}: SendApprovedSubmissionsToQueueAsync - No new submissions received from pom endpoint to queue", config.Value.LogPrefix);
                return;
            }

            await using var sender = serviceBusClient.CreateSender(config.Value.ObligationQueueName);

            var groupedSubmissions = approvedSubmissionEntities
                .GroupBy(s => s.SubmitterId)
                .Select(g => (SubmitterId: g.Key, Submissions: g.ToList()));

            foreach (var (submitterId, submissions) in groupedSubmissions)
            {
                logger.LogInformation("{LogPrefix}: SendApprovedSubmissionsToQueueAsync - Sending message to obligation queue: Submitter Id - {SubmitterId} with entity count {SubmissonsCount}", config.Value.LogPrefix, submitterId, submissions.Count);
                await sender.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(submissions, jsonOptions)));
            }

            logger.LogInformation("{LogPrefix}: SendApprovedSubmissionsToQueueAsync - Messages have been published to the obligation queue.", config.Value.LogPrefix);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{LogPrefix}: SendApprovedSubmissionsToQueueAsync - Error sending messages to queue {Message}", config.Value.LogPrefix, ex.Message);
            throw;
        }
    }
}