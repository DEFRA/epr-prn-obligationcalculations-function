using Azure.Messaging.ServiceBus;
using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EPR.PRN.ObligationCalculation.Application.Services;

public class ServiceBusProvider : IServiceBusProvider
{
    private readonly ILogger<ServiceBusProvider> _logger;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusConfig _config;
    private readonly string _logPrefix;

    public ServiceBusProvider(ILogger<ServiceBusProvider> logger, ServiceBusClient serviceBusClient, IOptions<ServiceBusConfig> config)
    {
        _logger = logger;
        _serviceBusClient = serviceBusClient;
        _config = config.Value;
        _logPrefix = nameof(ServiceBusProvider);
    }
    public async Task SendApprovedSubmissionsToQueueAsync(List<ApprovedSubmissionEntity> approvedSubmissionEntities)
    {
        try
        {
            if (approvedSubmissionEntities.Count == 0)
            {
                _logger.LogInformation("[{LogPrefix}]: No new submissions received from pom endpoint to queue", _logPrefix);
                return;
            }
            var organisationIds = approvedSubmissionEntities
                                    .Select(r => r.OrganisationId)
                                    .Distinct()
                                    .ToList();

            await using var sender = _serviceBusClient.CreateSender(_config.ObligationQueueName);
            using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();
            var options = new JsonSerializerOptions { WriteIndented = true };
            foreach (var organisationId in organisationIds)
            {
                var submissions = approvedSubmissionEntities.Where(s => s.OrganisationId == organisationId).ToList();
                var jsonSumissions = JsonSerializer.Serialize(submissions, options);
                if (!messageBatch.TryAddMessage(new ServiceBusMessage(jsonSumissions)))
                {
                    _logger.LogWarning("{LogPrefix} The message {OrganisationId} is too large to fit in the batch.", _logPrefix, organisationId);
                }
            }

            await sender.SendMessagesAsync(messageBatch);
            _logger.LogInformation("[{LogPrefix}]: A batch of {MessageBatchCount} messages has been published to the obligation queue.", _logPrefix, messageBatch.Count);
        }
        catch(Exception ex)
        {
            _logger.LogError("[{LogPrefix}]: Error sending messages to queue {Ex}", _logPrefix, ex);
            throw;
        }
    }

    public async Task<string?> GetLastSuccessfulRunDateFromQueue()
    {
        try
        {
            await using var receiver = _serviceBusClient.CreateReceiver(_config.ObligationLastSuccessfulRunQueueName);

            // Retrieve all messages from the queue
            var messages = await receiver.ReceiveMessagesAsync(int.MaxValue, TimeSpan.FromSeconds(10));

            if (messages == null || !messages.Any())
            {
                _logger.LogInformation("[{LogPrefix}]: No messages found to return last successful run date from the queue {QueueName}", _logPrefix, _config.ObligationLastSuccessfulRunQueueName);
                return null;
            }

            // Get the message with the latest sequence number
            var latestMessage = messages.OrderByDescending(m => m.SequenceNumber).First();
            string lastRunDate = latestMessage.Body.ToString();

            // Mark all messages as completed
            foreach (var message in messages)
            {
                await receiver.CompleteMessageAsync(message);
            }

            _logger.LogInformation("[{LogPrefix}]: Last run date {Date} received from queue", _logPrefix, lastRunDate);
            return lastRunDate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{LogPrefix}]: Error receiving message: {ex.Message}", _logPrefix, ex.Message);
            throw;
        }
    }

    public async Task SendSuccessfulRunDateToQueue(string runDate)
    {
        try
        {
            await using var sender = _serviceBusClient.CreateSender(_config.ObligationLastSuccessfulRunQueueName);
            var message = new ServiceBusMessage(runDate);
            await sender.SendMessageAsync(message);
            _logger.LogInformation("[{LogPrefix}]: Updated currect successful run date ({RunDate})to queue", _logPrefix, runDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{LogPrefix}]: Error whild sending runDate message: {Message}", _logPrefix, ex.Message);
            throw;
        }
    }
}
