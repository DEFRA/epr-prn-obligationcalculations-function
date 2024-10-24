#nullable disable
using AutoFixture;
using Azure.Messaging.ServiceBus;
using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using Moq;
using System;

namespace EPR.PRN.ObligationCalculation.Tests.Services;

[TestClass]
public class ServiceBusProviderTests
{
    private Mock<ILogger<ServiceBusProvider>> _loggerMock;
    private Mock<ServiceBusClient> _serviceBusClientMock;
    private Mock<IOptions<ServiceBusConfig>> _configMock;
    private Mock<ServiceBusSender> _serviceBusSenderMock;
    private ServiceBusProvider _serviceBusProvider;
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
        _mockReceiver = new Mock<ServiceBusReceiver>();

        var config = new ServiceBusConfig
        {
            ObligationQueueName = "test-queue-1",
            ObligationLastSuccessfulRunQueueName = "test-queue-2"
        };

        _configMock.Setup(c => c.Value).Returns(config);
        
        // Set up ServiceBusClient to return the mock receiver
        _serviceBusClientMock
            .Setup(client => client.CreateReceiver(It.IsAny<string>()))
            .Returns(_mockReceiver.Object);

        _serviceBusProvider = new ServiceBusProvider(
            _loggerMock.Object,
            _serviceBusClientMock.Object,
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
        _serviceBusSenderMock.Verify(r => r.DisposeAsync(), Times.Once);
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
        _serviceBusSenderMock.Verify(r => r.DisposeAsync(), Times.Once);
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
        var messageBatch = ServiceBusModelFactory.ServiceBusMessageBatch(500, []);

        _serviceBusSenderMock.Setup(sender => sender.CreateMessageBatchAsync(default)).ReturnsAsync(messageBatch);
        _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);

        var approvedSubmissions = new List<ApprovedSubmissionEntity>();
        
        // Act
        await _serviceBusProvider.SendApprovedSubmissionsToQueueAsync(approvedSubmissions);

        // Assert
        _serviceBusSenderMock.Verify(r => r.DisposeAsync(), Times.Once);
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
    [ExpectedException(typeof(Exception))]
    public async Task SendApprovedSubmissionsToQueueAsync_ShouldThrowError_WHenClientFails()
    {
        // Arrange
        var approvedSubmissions = fixture.CreateMany<ApprovedSubmissionEntity>(3).ToList();

        var messageBatch = ServiceBusModelFactory.ServiceBusMessageBatch(500, []);

        _serviceBusSenderMock.Setup(sender => sender.CreateMessageBatchAsync(default)).ReturnsAsync(messageBatch);
        _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Throws(new Exception("error"));

        // Act
        await _serviceBusProvider.SendApprovedSubmissionsToQueueAsync(approvedSubmissions);

        // Assert
        _serviceBusSenderMock.Verify(r => r.DisposeAsync(), Times.Once);
        _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
    }

    [TestMethod]
    public async Task GetLastSuccessfulRunDateFromQueue_Returns_LastSuccesfulRunDate()
    {
        // Arrange
        var lastSuccessfulRunDate = "2024-10-10";
        var message = CreateMessage(lastSuccessfulRunDate);

        _mockReceiver.SetupSequence(r => r.ReceiveMessageAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);
        
        // Act
        var result = await _serviceBusProvider.GetLastSuccessfulRunDateFromQueue();

        // Assert
        _mockReceiver.Verify(r => r.DisposeAsync(), Times.Once);
        Assert.IsNotNull(result);
        Assert.AreEqual(lastSuccessfulRunDate, result);

    }

    [TestMethod]
    public async Task GetLastSuccessfulRunDateFromQueue_Returns_Null()
    {
        // Arrange
        _mockReceiver.SetupSequence(r => r.ReceiveMessageAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceBusReceivedMessage)null);

        // Act
        var result = await _serviceBusProvider.GetLastSuccessfulRunDateFromQueue();

        // Assert
        _mockReceiver.Verify(r => r.DisposeAsync(), Times.Once);
        Assert.IsNull(result);
    }



    [TestMethod]
    public async Task GetLastSuccessfulRunDateFromQueue_ThrowsException()
    {
        // Arrange
        var exception = new ServiceBusException("Test exception", ServiceBusFailureReason.GeneralError);
        _mockReceiver.SetupSequence(r => r.ReceiveMessageAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ServiceBusException>(() => _serviceBusProvider.GetLastSuccessfulRunDateFromQueue());
        
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

        _mockReceiver.Verify(sender => sender.DisposeAsync(), Times.Once);
    }

    [TestMethod]
    public async Task SendSuccessfulRunDateToQueue_ShouldSendMessageSuccessfully()
    {
        // Arrange
        var runDate = "2024-10-10";
        _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>()))
                                 .Returns(_serviceBusSenderMock.Object);

        // Act
        await _serviceBusProvider.SendSuccessfulRunDateToQueue(runDate);

        // Assert
        _serviceBusSenderMock.Verify(sender => sender.SendMessageAsync(It.Is<ServiceBusMessage>(msg => msg.Body.ToString() == runDate), default), Times.Once);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

        _serviceBusSenderMock.Verify(sender => sender.DisposeAsync(), Times.Once);
    }

    [TestMethod]
    public async Task SendSuccessfulRunDateToQueue_ShouldLogErrorAndThrow_WhenServiceBusExceptionOccurs()
    {
        // Arrange
        var runDate = "2024-10-10";
        var exception = new ServiceBusException("Test exception", ServiceBusFailureReason.GeneralError);

        _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>()))
                                 .Returns(_serviceBusSenderMock.Object);
        _serviceBusSenderMock.Setup(sender => sender.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default))
                             .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ServiceBusException>(() => _serviceBusProvider.SendSuccessfulRunDateToQueue(runDate));

        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),Times.Once);

        _serviceBusSenderMock.Verify(sender => sender.DisposeAsync(), Times.Once);
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