using Azure.Messaging.ServiceBus;
using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

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
            var submitterIds = approvedSubmissionEntities
                                    .Select(r => r.SubmitterId)
                                    .Distinct()
                                    .ToList();

            await using var sender = serviceBusClient.CreateSender(config.Value.ObligationQueueName);

            foreach (var submitterId in submitterIds)
            {
                var submissions = approvedSubmissionEntities.Where(s => s.SubmitterId == submitterId).ToList();
                var jsonSumissions = JsonSerializer.Serialize(submissions, jsonOptions);
				if (submitterId == Guid.Parse("9B2647DB-210A-4BCB-86A1-E68E210A8F42"))
				{
					logger.LogInformation("{LogPrefix}: SendApprovedSubmissionsToQueueAsync - Sending message to obligation queue: Submitter Id - {SubmitterId} with submissions {SubmissonsCount}", config.Value.LogPrefix, submitterId, jsonSumissions);
				}
				logger.LogInformation("{LogPrefix}: SendApprovedSubmissionsToQueueAsync - Sending message to obligation queue: Submitter Id - {SubmitterId} with entity count {SubmissonsCount}", config.Value.LogPrefix, submitterId, submissions.Count);

				await sender.SendMessageAsync(new ServiceBusMessage(jsonSumissions));
            }

            logger.LogInformation("{LogPrefix}: SendApprovedSubmissionsToQueueAsync - Messages have been published to the obligation queue.", config.Value.LogPrefix);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{LogPrefix}: SendApprovedSubmissionsToQueueAsync - Error sending messages to queue {Message}", config.Value.LogPrefix, ex.Message);
            throw;
        }
    }

    public async Task<string?> GetLastSuccessfulRunDateFromQueue()
    {
        try
        {
            await using var receiver = serviceBusClient.CreateReceiver(config.Value.ObligationLastSuccessfulRunQueueName);

            // Retrieve all messages from the queue
            var messages = await receiver.ReceiveMessagesAsync(int.MaxValue, TimeSpan.FromSeconds(10));
            if (messages == null || !messages.Any())
            {
                logger.LogInformation("{LogPrefix}: GetLastSuccessfulRunDateFromQueue - No messages found to return last successful run date from the queue {QueueName}", config.Value.LogPrefix, config.Value.ObligationLastSuccessfulRunQueueName);
                return null;
            }
            logger.LogInformation("{LogPrefix}: GetLastSuccessfulRunDateFromQueue - Messages received {Messages} from the queue {QueueName}", config.Value.LogPrefix, JsonSerializer.Serialize(messages, jsonOptions), config.Value.ObligationLastSuccessfulRunQueueName);

            // Get the message with the latest sequence number
            var latestMessage = messages.OrderByDescending(m => m.SequenceNumber).First();
            string lastRunDate = latestMessage.Body.ToString();

            // Mark all messages as completed
            foreach (var message in messages)
            {
                await receiver.CompleteMessageAsync(message);
            }

            logger.LogInformation("{LogPrefix}: GetLastSuccessfulRunDateFromQueue - Last run date {Date} received from queue", config.Value.LogPrefix, lastRunDate);
            return lastRunDate;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{LogPrefix}: GetLastSuccessfulRunDateFromQueue - Error receiving message: {Message}", config.Value.LogPrefix, ex.Message);
            throw;
        }
    }

    public async Task SendSuccessfulRunDateToQueue(string runDate)
    {
        try
        {
            await using var sender = serviceBusClient.CreateSender(config.Value.ObligationLastSuccessfulRunQueueName);
            var message = new ServiceBusMessage(runDate);
            await sender.SendMessageAsync(message);
            logger.LogInformation("{LogPrefix}: SendSuccessfulRunDateToQueue: Updated currect successful run date ({RunDate})to queue", config.Value.LogPrefix, runDate);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{LogPrefix}: SendSuccessfulRunDateToQueue: Error whild sending runDate message: {Message}", config.Value.LogPrefix, ex.Message);
            throw;
        }
    }
}