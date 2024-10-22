#nullable disable
using AutoFixture;
using Azure.Messaging.ServiceBus;
using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;

namespace EPR.PRN.ObligationCalculation.Tests.Services;

[TestClass]
public class ServiceBusProviderTests
{
    private Mock<ILogger<ServiceBusProvider>> _loggerMock;
    private Mock<ServiceBusClient> _serviceBusClientMock;
    private Mock<IOptions<ServiceBusConfig>> _configMock;
    private Mock<ServiceBusSender> _serviceBusSenderMock;
    private ServiceBusProvider _serviceBusProvider;
    private Mock<IPrnService> _prnServiceMock;
    private Mock<ServiceBusReceiver> _mockReceiver;
    private Fixture fixture;


    [TestInitialize]
    public void TestInitialize()
    {
        fixture = new Fixture();
        _loggerMock = new Mock<ILogger<ServiceBusProvider>>();
        _serviceBusClientMock = new Mock<ServiceBusClient>();
        _configMock = new Mock<IOptions<ServiceBusConfig>>();
        _serviceBusSenderMock = new Mock<ServiceBusSender>();
        _prnServiceMock = new Mock<IPrnService>();
        _mockReceiver = new Mock<ServiceBusReceiver>();

        var config = new ServiceBusConfig { QueueName = "test-queue" };
        _configMock.Setup(c => c.Value).Returns(config);
        
        // Set up ServiceBusClient to return the mock receiver
        _serviceBusClientMock
            .Setup(client => client.CreateReceiver(It.IsAny<string>()))
            .Returns(_mockReceiver.Object);

        _serviceBusProvider = new ServiceBusProvider(
            _loggerMock.Object,
            _serviceBusClientMock.Object,
            _prnServiceMock.Object,
            _configMock.Object
        );
    }

    [TestMethod]
    public async Task SendApprovedSubmissionsToQueueAsync_Success()
    {
        // Arrange
        var messageBatch = ServiceBusModelFactory.ServiceBusMessageBatch(500, []);

        _serviceBusSenderMock.Setup(sender => sender.CreateMessageBatchAsync(default)).ReturnsAsync(messageBatch);
        _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);

        var approvedSubmissions = fixture.CreateMany<ApprovedSubmissionEntity>(3).ToList();

        // Act
        await _serviceBusProvider.SendApprovedSubmissionsToQueueAsync(approvedSubmissions);

        // Assert
        _serviceBusSenderMock.Verify(sender => sender.CreateMessageBatchAsync(default), Times.Once);
        _serviceBusSenderMock.Verify(sender => sender.SendMessagesAsync(It.IsAny<ServiceBusMessageBatch>(), default), Times.Once);
        _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
    }

    [TestMethod]
    public async Task SendApprovedSubmissionsToQueueAsync_MessageTooMany_Warns()
    {
        // Arrange
        var approvedSubmissions = fixture.Build<ApprovedSubmissionEntity>()
            .With(x => x.OrganisationId, 123)
            .CreateMany(10)
            .ToList();
        int messageCountThreshold = 1;
        List<ServiceBusMessage> messageList = [];
        messageList.Add(new ServiceBusMessage());
        ServiceBusMessageBatch messageBatch = ServiceBusModelFactory.ServiceBusMessageBatch(
            batchSizeBytes: 500,
            batchMessageStore: messageList,
            batchOptions: new CreateMessageBatchOptions(),
            tryAddCallback: _ => messageList.Count < messageCountThreshold);
        _serviceBusSenderMock.Setup(sender => sender.CreateMessageBatchAsync(default)).ReturnsAsync(messageBatch);
        _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);
        // Act
        await _serviceBusProvider.SendApprovedSubmissionsToQueueAsync(approvedSubmissions);

        // Assert
        _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
    }

    [TestMethod]
    public async Task SendApprovedSubmissionsToQueueAsync_LogInformation_WhenNoSubmissions()
    {
        // Arrange
        var approvedSubmissions = new List<ApprovedSubmissionEntity>();
        
        // Act
        await _serviceBusProvider.SendApprovedSubmissionsToQueueAsync(approvedSubmissions);

        // Assert
        _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
    }

    [TestMethod]
    public async Task SendApprovedSubmissionsToQueueAsync_ShouldThrowError_WHenClientFails()
    {
        // Arrange
        var approvedSubmissions = new List<ApprovedSubmissionEntity>();
        _serviceBusSenderMock.Setup(sender => sender.CreateMessageBatchAsync(default)).ThrowsAsync(new Exception("error"));

        // Act
        await _serviceBusProvider.SendApprovedSubmissionsToQueueAsync(approvedSubmissions);

        // Assert
        _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
    }

    [TestMethod]
    public async Task ReceiveAndProcessMessagesFromQueueAsync_ShouldProcessMessages()
    {
        // Arrange
        var message1 = fixture.Build<ApprovedSubmissionEntity>()
                                .With(r => r.OrganisationId, 1)
                                .CreateMany(10);
        var message2 = fixture.Build<ApprovedSubmissionEntity>()
                                .With(r => r.OrganisationId, 2)
                                .CreateMany(5);

        var messages = new List<ServiceBusReceivedMessage>
        {
            CreateMessage(JsonConvert.SerializeObject(message1)),
            CreateMessage(JsonConvert.SerializeObject(message2))
        };

        // Setup receiver to return messages and then an empty list (to stop the loop)
        _mockReceiver.SetupSequence(r => r.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages)  // First call returns two messages
            .ReturnsAsync([]);  // Second call returns no messages

        // Act
        await _serviceBusProvider.ReceiveAndProcessMessagesFromQueueAsync();

        // Assert
        _prnServiceMock.Verify(p => p.ProcessApprovedSubmission(It.IsAny<string>()), Times.Exactly(2));  // Ensure both messages were processed
        _mockReceiver.Verify(r => r.CompleteMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));  // Ensure both messages were completed
    }

    [TestMethod]
    public async Task ReceiveAndProcessMessagesFromQueueAsync_ShouldAbandonMessageOnError()
    {
        // Arrange
        var message1 = fixture.Build<ApprovedSubmissionEntity>()
                                .With(r => r.OrganisationId, 2)
                                .CreateMany(5);
        var messages = new List<ServiceBusReceivedMessage>
        {
            CreateMessage(JsonConvert.SerializeObject(message1))
        };

        // Setup receiver to return one message
        _mockReceiver.SetupSequence(r => r.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages)
            .ReturnsAsync([]);

        // Setup ProcessApprovedSubmission to throw an exception
        _prnServiceMock.Setup(p => p.ProcessApprovedSubmission(It.IsAny<string>())).ThrowsAsync(new Exception("Processing error"));

        // Act
        await _serviceBusProvider.ReceiveAndProcessMessagesFromQueueAsync();

        // Assert
        _mockReceiver.Verify(r => r.AbandonMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>()), Times.Once);  // Ensure the message was abandoned
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);  // Ensure error was logged
    }

    [TestMethod]
    public async Task ReceiveAndProcessMessagesFromQueueAsync_ShouldDisposeReceiver()
    {
        // Arrange
        _mockReceiver.Setup(r => r.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServiceBusReceivedMessage>());  // No messages, to stop the loop immediately

        // Act
        await _serviceBusProvider.ReceiveAndProcessMessagesFromQueueAsync();

        // Assert
        _mockReceiver.Verify(r => r.DisposeAsync(), Times.Once);  // Ensure receiver is disposed
    }

    // Helper method to create a real ServiceBusReceivedMessage using ServiceBusModelFactory
    private ServiceBusReceivedMessage CreateMessage(string body)
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