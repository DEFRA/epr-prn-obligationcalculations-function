using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using EPR.PRN.ObligationCalculation.Application.Services;

namespace EPR.PRN.ObligationCalculation.Function;

public class ProcessApprovedSubmissionsFunction
{
    private readonly IPrnService _prnService;
    private readonly ILogger<ProcessApprovedSubmissionsFunction> _logger;
    private readonly string _logPrefix;

    public ProcessApprovedSubmissionsFunction(ILogger<ProcessApprovedSubmissionsFunction> logger, IPrnService prnService)
    {
        _logger = logger;
        _prnService = prnService;
        _logPrefix = nameof(ProcessApprovedSubmissionsFunction);
    }

    [Function("ProcessApprovedSubmissionsFunction")]
    public async Task RunAsync([ServiceBusTrigger("%ServiceBus:ObligationQueueName%", Connection = "ServiceBus")] ServiceBusReceivedMessage message)
    {
        try
        {
            _logger.LogInformation("[{LogPrefix}]: Received message with ID: {MessageId}", _logPrefix, message.MessageId);
            string messageBody = message.Body.ToString();
            await _prnService.ProcessApprovedSubmission(messageBody);
            _logger.LogInformation("[{LogPrefix}]: Processed message with ID: {MessageId}", _logPrefix, message.MessageId);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{LogPrefix}]: Exception occurred while processing message: {Message}", _logPrefix, ex.Message);
            throw;
        }
    }
}
