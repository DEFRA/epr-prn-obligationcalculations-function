﻿#nullable disable
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Newtonsoft.Json;
using Moq;
using Moq.Protected;
using EPR.PRN.ObligationCalculation.Application.DTOs;

namespace EPR.PRN.ObligationCalculation.Application.UnitTests.Services;

[TestClass]
public class PrnServiceTests
{
    private Mock<ILogger<PrnService>> _loggerMock;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private Mock<IOptions<CommonBackendApiConfig>> _configMock;
    private HttpClient _httpClient;
    private PrnService _prnService;
    private CommonBackendApiConfig _config;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<PrnService>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _configMock = new Mock<IOptions<CommonBackendApiConfig>>();

        // Setup config
        _config = new CommonBackendApiConfig
        {
            BaseUrl = "http://test-url.com/",
            PrnCalculateEndPoint = "api/calculate/{0}"
        };

        _configMock.Setup(c => c.Value).Returns(_config);

        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri(_configMock.Object.Value.BaseUrl)
        };

        _prnService = new PrnService(_loggerMock.Object, _httpClient, _configMock.Object);
    }

    [TestMethod]
    public async Task ProcessApprovedSubmission_ShouldLogInformation_WhenSubmissionIsEmpty()
    {
        // Arrange
        string emptySubmission = string.Empty;

        // Act
        await _prnService.ProcessApprovedSubmission(emptySubmission);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Exactly(1));
    }

    [TestMethod]
    public async Task ProcessApprovedSubmission_ShouldSendPostRequest_WhenSubmissionIsNotEmpty()
    {
        // Arrange
        var submission = JsonConvert.SerializeObject(new List<ApprovedSubmissionEntity>
        {
            new () { SubmissionId = Guid.NewGuid() }
        });

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        // Act
        await _prnService.ProcessApprovedSubmission(submission);

        // Assert
        _httpMessageHandlerMock
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Post && r.RequestUri.ToString().Contains(_config.BaseUrl)),
                ItExpr.IsAny<CancellationToken>());
    }

    [TestMethod]
    [ExpectedException(typeof(HttpRequestException))]
    public async Task ProcessApprovedSubmission_ShouldThrowHttpRequestException_WhenUnsuccesfulResponse()
    {
        // Arrange
        var submission = JsonConvert.SerializeObject(new List<ApprovedSubmissionEntity>
        {
            new() { SubmissionId = Guid.NewGuid() }
        });

        var expectedLogMessage = "Error while submitting submissions data";

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        await _prnService.ProcessApprovedSubmission(submission);

        // Assert handled by ExpectedException
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedLogMessage)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()));
    }

    [TestMethod]
    [ExpectedException(typeof(Exception))]
    public async Task ProcessApprovedSubmission_ShouldThrowException_WhenHttpClientThrowsException()
    {
        // Arrange
        var submission = JsonConvert.SerializeObject(new List<ApprovedSubmissionEntity>
        {
            new() { SubmissionId = Guid.NewGuid() }
        });

        var expectedLogMessage = "Error while submitting submissions data";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new Exception("Test Exception"));

        // Act
        await _prnService.ProcessApprovedSubmission(submission);

        // Assert handled by ExpectedException
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedLogMessage)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()));
    }
}