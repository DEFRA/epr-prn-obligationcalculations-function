using Azure.Messaging.ServiceBus;

namespace EPR.PRN.ObligationCalculation.Application.UnitTests.Helpers;

public static class ServiceBusModelBuilder
{
    public static ServiceBusReceivedMessage CreateServiceBusReceivedMessage(string body)
    {
        var binaryData = new BinaryData(body);

        return ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: binaryData,
            messageId: Guid.NewGuid().ToString(),
            contentType: "application/json",
            correlationId: null,
            subject: null,
            replyTo: null,
            replyToSessionId: null,
            sessionId: null,
            partitionKey: null,
            viaPartitionKey: null,
            to: null,
            timeToLive: TimeSpan.FromMinutes(2),
            scheduledEnqueueTime: DateTimeOffset.UtcNow,
            lockTokenGuid: Guid.NewGuid(),
            sequenceNumber: 1,
            enqueuedSequenceNumber: 1,
            enqueuedTime: DateTimeOffset.UtcNow,
            lockedUntil: DateTimeOffset.UtcNow.AddMinutes(5),
            deadLetterSource: null);
    }
}
