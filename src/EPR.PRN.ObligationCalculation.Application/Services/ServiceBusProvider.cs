#nullable disable

using Azure.Identity;
using Azure.Messaging.ServiceBus;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EPR.PRN.ObligationCalculation.Application.Services
{
    public class ServiceBusProvider : IServiceBusProvider
    {
        private readonly ILogger _logger;
        private readonly ServiceBusClient _serviceBusClient;

        private readonly string _serviceBusQueueName = string.Empty;

        public ServiceBusProvider(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<SubmissionsDataService>();
            _serviceBusQueueName = configuration["ServiceBusQueueName"] ?? throw new InvalidOperationException("ServiceBusQueueName configuration is missing.");
            var serviceBusConnectionString = configuration["ServiceBusConnectionString"] ?? throw new InvalidOperationException("ServiceBusConnectionString configuration is missing.");
            var serviceBusClientOptions = new ServiceBusClientOptions
            {
                TransportType = ServiceBusTransportType.AmqpWebSockets
            };
            _serviceBusClient = new ServiceBusClient(serviceBusConnectionString, serviceBusClientOptions);
        }
        public async Task SendApprovedSubmissionsToQueue(List<ApprovedSubmissionEntity> approvedSubmissionEntities)
        {
            var organisationIds = approvedSubmissionEntities
                                    .Select(r => r.OrganisationId)
                                    .Distinct()
                                    .ToList();

            ServiceBusSender sender = _serviceBusClient.CreateSender(_serviceBusQueueName);
            using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();

            foreach (var organisationId in organisationIds)
            {
                var submissions = approvedSubmissionEntities.Where(s => s.OrganisationId == organisationId).ToList();
                if (submissions.Count != 0)
                {
                    var jsonSumissions = JsonSerializer.Serialize(submissions, new JsonSerializerOptions { WriteIndented = true });
                    if (!messageBatch.TryAddMessage(new ServiceBusMessage(jsonSumissions)))
                    {
                        _logger.LogWarning("The message {OrganisationId} is too large to fit in the batch.", organisationId);
                    }
                }
            }

            await sender.SendMessagesAsync(messageBatch);
            _logger.LogInformation("A batch of {MessageBatchCount} messages has been published to the queue.", messageBatch.Count);
        }
    }
}
