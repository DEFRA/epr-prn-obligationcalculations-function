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
            var organisationIds = approvedSubmissionEntities
                                    .Select(r => r.OrganisationId)
                                    .Distinct()
                                    .ToList();

            await using var sender = serviceBusClient.CreateSender(config.Value.ObligationQueueName);
            using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();
            foreach (var organisationId in organisationIds)
            {
                var submissions = approvedSubmissionEntities.Where(s => s.OrganisationId == organisationId).ToList();
                var jsonSumissions = JsonSerializer.Serialize(submissions, jsonOptions);
                if (!messageBatch.TryAddMessage(new ServiceBusMessage(jsonSumissions)))
                {
                    logger.LogWarning("{LogPrefix}: SendApprovedSubmissionsToQueueAsync - The message {OrganisationId} is too large to fit in the batch.", config.Value.LogPrefix, organisationId);
                }
            }

            await sender.SendMessagesAsync(messageBatch);
            logger.LogInformation("{LogPrefix}: SendApprovedSubmissionsToQueueAsync - A batch of {MessageBatchCount} messages has been published to the obligation queue.", config.Value.LogPrefix, messageBatch.Count);
        }
        catch(Exception ex)
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
            logger.LogInformation("{LogPrefix}: Updated currect successful run date ({RunDate})to queue", config.Value.LogPrefix, runDate);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{LogPrefix}: Error whild sending runDate message: {Message}", config.Value.LogPrefix, ex.Message);
            throw;
        }
    }
}
