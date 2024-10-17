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

    public ServiceBusProvider(ILogger<ServiceBusProvider> logger, ServiceBusClient serviceBusClient, IOptions<ServiceBusConfig> config)
    {
        _logger = logger;
        _serviceBusClient = serviceBusClient;
        _config = config.Value;
    }
    public async Task SendApprovedSubmissionsToQueue(List<ApprovedSubmissionEntity> approvedSubmissionEntities)
    {
        var organisationIds = approvedSubmissionEntities
                                .Select(r => r.OrganisationId)
                                .Distinct()
                                .ToList();

        var sender = _serviceBusClient.CreateSender(_config.QueueName);
        using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();
        var options = new JsonSerializerOptions { WriteIndented = true };
        foreach (var organisationId in organisationIds)
        {
            var submissions = approvedSubmissionEntities.Where(s => s.OrganisationId == organisationId).ToList();
            if (submissions.Count != 0)
            {
                var jsonSumissions = JsonSerializer.Serialize(submissions, options);
                if (!messageBatch.TryAddMessage(new ServiceBusMessage(jsonSumissions)))
                {
                    _logger.LogWarning("{LogPrefix} The message {OrganisationId} is too large to fit in the batch.", ApplicationConstants.StoreApprovedSubmissionsFunctionLogPrefix, organisationId);
                }
            }
        }

        await sender.SendMessagesAsync(messageBatch);
        _logger.LogInformation("{LogPrefix} A batch of {MessageBatchCount} messages has been published to the queue.", ApplicationConstants.StoreApprovedSubmissionsFunctionLogPrefix, messageBatch.Count);
    }

    public async Task ReceiveAndProcessMessagesFromQueueAsync()
    {
        var receiver = _serviceBusClient.CreateReceiver(_config.QueueName);
        bool continueReceiving = true;

        try
        {
            while (continueReceiving)
            {
                var receivedMessages = await receiver.ReceiveMessagesAsync(maxMessages: 50, TimeSpan.FromSeconds(2));

                if (receivedMessages.Count == 0)
                {
                    continueReceiving = false;
                }
                else
                {
                    foreach (var message in receivedMessages)
                    {
                        try
                        {
                            // Process message
                            var result = message.Body.ToString();
                            // call api
                            await receiver.CompleteMessageAsync(message);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error processing message: {message.MessageId}");
                            await receiver.AbandonMessageAsync(message);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving messages from queue");
            throw;
        }
        finally
        {
            await receiver.DisposeAsync();
        }
    }
}
