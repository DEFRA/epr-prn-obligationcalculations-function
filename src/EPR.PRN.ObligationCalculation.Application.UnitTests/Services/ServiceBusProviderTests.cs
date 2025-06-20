using AutoFixture;
using Azure.Messaging.ServiceBus;
using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.DTOs;
using EPR.PRN.ObligationCalculation.Application.Services;
using EPR.PRN.ObligationCalculation.Application.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EPR.PRN.ObligationCalculation.Application.UnitTests.Services;

[TestClass]
public class ServiceBusProviderTests
{
    private Mock<ILogger<ServiceBusProvider>> _loggerMock = null!;
    private Mock<ServiceBusClient> _serviceBusClientMock = null!;
    private Mock<IOptions<ServiceBusConfig>> _configMock = null!;
    private Mock<ServiceBusSender> _serviceBusSenderMock = null!;
    private ServiceBusProvider _serviceBusProvider = null!;
    private Mock<ServiceBusReceiver> _serviceBusReceiverMock = null!;
    private Fixture fixture = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        fixture = new Fixture();
        _loggerMock = new Mock<ILogger<ServiceBusProvider>>();
        _serviceBusClientMock = new Mock<ServiceBusClient>();
        _configMock = new Mock<IOptions<ServiceBusConfig>>();
        _serviceBusSenderMock = new Mock<ServiceBusSender>();
        _serviceBusReceiverMock = new Mock<ServiceBusReceiver>();

        var config = new ServiceBusConfig
        {
            ObligationQueueName = "test-queue-1",
            ObligationLastSuccessfulRunQueueName = "test-queue-2"
        };

        _configMock.Setup(c => c.Value).Returns(config);
        
        // Set up ServiceBusClient to return the mock receiver
        _serviceBusClientMock
            .Setup(client => client.CreateReceiver(It.IsAny<string>()))
            .Returns(_serviceBusReceiverMock.Object);

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
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [TestMethod]
    public async Task SendApprovedSubmissionsToQueueAsync_MessageTooMany_Warns()
    {
        // Arrange
        var approvedSubmissions = fixture.Build<ApprovedSubmissionEntity>()
            .With(x => x.SubmitterId, Guid.NewGuid())
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
        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
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
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [TestMethod]
    [ExpectedException(typeof(Exception))]
    public async Task SendApprovedSubmissionsToQueueAsync_ShouldThrowError_WHenClientFails()
    {
        // Arrange
        var approvedSubmissions = fixture.CreateMany<ApprovedSubmissionEntity>(3).ToList();

        _serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);
        _serviceBusSenderMock.Setup(sender => sender.CreateMessageBatchAsync(default)).ThrowsAsync(new Exception("error"));
        // Act
        await _serviceBusProvider.SendApprovedSubmissionsToQueueAsync(approvedSubmissions);

        // Assert
        _serviceBusSenderMock.Verify(r => r.DisposeAsync(), Times.Once);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [TestMethod]
    public async Task GetLastSuccessfulRunDateFromQueue_Returns_LatestMessageBasedOnSequence()
    {
        // Arrange
        var date1 = "2024-10-08";
        var date2 = "2024-10-09";
        var latestDate = "2024-10-10";

        var message1 = ServiceBusModelBuilder.CreateServiceBusReceivedMessage(date1, 1);
        var message2 = ServiceBusModelBuilder.CreateServiceBusReceivedMessage(date2, 2);
        var latestMessage = ServiceBusModelBuilder.CreateServiceBusReceivedMessage(latestDate, 3);
                
        _serviceBusReceiverMock
            .Setup(r => r.ReceiveMessagesAsync(
                It.IsAny<int>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([message1, message2, latestMessage]);

        
        int completeMessageCalls = 0;
        _serviceBusReceiverMock
            .Setup(r => r.CompleteMessageAsync(
                It.IsAny<ServiceBusReceivedMessage>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => completeMessageCalls++)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _serviceBusProvider.GetLastSuccessfulRunDateFromQueue();

        // Assert
        result.Should().Be(latestDate);  // The date from the message with the highest sequence number

        _serviceBusReceiverMock.Verify(r => r.CompleteMessageAsync(
                It.IsAny<ServiceBusReceivedMessage>(),
                It.IsAny<CancellationToken>()), Times.Exactly(completeMessageCalls));

        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);

        _serviceBusReceiverMock.Verify(r => r.DisposeAsync(), Times.AtLeastOnce);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(null)]
    public async Task GetLastSuccessfulRunDateFromQueue_Returns_Null_WhenNoMessages(int? messages)
    {
        // Arrange
        _serviceBusReceiverMock
            .Setup(r => r.ReceiveMessagesAsync(
                It.IsAny<int>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages == 0 ? [] : null);

        // Act
        var result = await _serviceBusProvider.GetLastSuccessfulRunDateFromQueue();

        // Assert
        result.Should().BeNull();

        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        _serviceBusReceiverMock.Verify(r => r.DisposeAsync(), Times.Once);
    }

    [TestMethod]
    public async Task GetLastSuccessfulRunDateFromQueue_ThrowsException()
    {
        // Arrange
        var exception = new Exception("Test exception");
        _serviceBusReceiverMock
            .Setup(r => r.ReceiveMessagesAsync(
                It.IsAny<int>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<Exception>(() => _serviceBusProvider.GetLastSuccessfulRunDateFromQueue());

        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        _serviceBusReceiverMock.Verify(sender => sender.DisposeAsync(), Times.Once);
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
        _serviceBusSenderMock.Verify(sender => sender.SendMessageAsync(
            It.Is<ServiceBusMessage>(msg => msg.Body.ToString() == runDate),
            default), Times.Once);
        
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        _serviceBusSenderMock.Verify(sender => sender.DisposeAsync(), Times.Once);
    }

    [TestMethod]
    public async Task SendSuccessfulRunDateToQueue_ShouldLogErrorAndThrow_WhenServiceBusExceptionOccurs()
    {
        // Arrange
        var runDate = "2024-10-10";
        var exception = new Exception("Test exception");
		var expectedErrorMessagePart = "SendSuccessfulRunDateToQueue: Error whild sending runDate message";

		_serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>()))
                                 .Returns(_serviceBusSenderMock.Object);

        _serviceBusSenderMock.Setup(sender => sender.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default))
                             .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<Exception>(() => _serviceBusProvider.SendSuccessfulRunDateToQueue(runDate));

        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
			It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains(expectedErrorMessagePart)),
			exception,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        _serviceBusSenderMock.Verify(sender => sender.DisposeAsync(), Times.Once);
    }

	[TestMethod]
	public async Task SendApprovedSubmissionsToQueueAsync_ShouldThrowError_WhenClientFails()
	{
		// Arrange
		var expectedErrorMessagePart = "SendApprovedSubmissionsToQueueAsync - Error sending messages to queue";
		var approvedSubmissions = fixture.CreateMany<ApprovedSubmissionEntity>(3).ToList();

		_serviceBusClientMock.Setup(client => client.CreateSender(It.IsAny<string>())).Returns(_serviceBusSenderMock.Object);
		_serviceBusSenderMock.Setup(sender => sender.CreateMessageBatchAsync(default)).ThrowsAsync(new Exception("error"));

		// Act & Assert
		await Assert.ThrowsExceptionAsync<Exception>(async () =>
		{
			await _serviceBusProvider.SendApprovedSubmissionsToQueueAsync(approvedSubmissions);
		});

		_serviceBusSenderMock.Verify(r => r.DisposeAsync(), Times.Once);
		_loggerMock.Verify(l => l.Log(
			LogLevel.Error,
			It.IsAny<EventId>(),
			It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains(expectedErrorMessagePart)),
			It.IsAny<Exception>(),
			It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
	}
}