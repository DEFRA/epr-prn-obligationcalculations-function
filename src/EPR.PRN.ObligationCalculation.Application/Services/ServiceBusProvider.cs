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
        var sender = _serviceBusClient.CreateSender(_config.ObligationQueueName);
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
                        _logger.LogWarning("{LogPrefix} The message {OrganisationId} is too large to fit in the batch.", _logPrefix, organisationId);
                    }
                }
            }

            await sender.SendMessagesAsync(messageBatch);
            _logger.LogInformation("{LogPrefix} A batch of {MessageBatchCount} messages has been published to the queue.", _logPrefix, messageBatch.Count);
        }
        catch(Exception ex)
        {
            _logger.LogError("[{LogPrefix}]: Error sending messages to queue {Ex}", _logPrefix, ex);
            throw;
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }

    public async Task<string?> GetLastSuccessfulRunDateFromQueue()
    {
        var receiver = _serviceBusClient.CreateReceiver(_config.ObligationLastSuccessfulRunQueueName);
        try
        {
            var message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));

            if (message == null)
            {
                return null;
            }
            string runDate = message.Body.ToString();
            await receiver.CompleteMessageAsync(message);
            return runDate;
        }
        catch (ServiceBusException ex)
        {
            _logger.LogError(ex, "[{LogPrefix}]: Error receiving message: {ex.Message}", _logPrefix, ex.Message);
            throw;
        }
        finally
        {
            await receiver.DisposeAsync();
        }
    }

    public async Task SendSuccessfulRunDateToQueue(string runDate)
    {
        var sender = _serviceBusClient.CreateSender(_config.ObligationLastSuccessfulRunQueueName);
        try
        {
            var message = new ServiceBusMessage(runDate);
            await sender.SendMessageAsync(message);
            _logger.LogInformation("[{LogPrefix}]: Run date sent to queue: {MessageBody}", _logger, message.Body);
        }
        catch (ServiceBusException ex)
        {
            _logger.LogError(ex, "[{LogPrefix}]: Error whild sending runDate message: {Message}", _logPrefix, ex.Message);
            throw;
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }
}
