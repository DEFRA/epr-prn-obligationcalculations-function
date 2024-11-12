using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using EPR.PRN.ObligationCalculation.Application.Services;
using EPR.PRN.ObligationCalculation.Application.Configs;
using Microsoft.Extensions.Options;

namespace EPR.PRN.ObligationCalculation.Function;

public class ProcessApprovedSubmissionsFunction(ILogger<ProcessApprovedSubmissionsFunction> logger, IPrnService prnService, IOptions<ApplicationConfig> config)
{
    [Function("ProcessApprovedSubmissionsFunction")]
    public async Task RunAsync([ServiceBusTrigger("%ServiceBus:ObligationQueueName%", Connection = "ServiceBus")] ServiceBusReceivedMessage message)
    {
        try
        {
            logger.LogInformation("{LogPrefix}: ProcessApprovedSubmissionsFunction: Received message with ID: {MessageId}", config.Value.LogPrefix, message.MessageId);
            string messageBody = message.Body.ToString();
            await prnService.ProcessApprovedSubmission(messageBody);
            logger.LogInformation("{LogPrefix}: ProcessApprovedSubmissionsFunction: Processed message with ID: {MessageId}", config.Value.LogPrefix, message.MessageId);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{LogPrefix}: ProcessApprovedSubmissionsFunction: Exception occurred while processing message: {Message}", config.Value.LogPrefix, ex.Message);
            throw;
        }
    }
}
