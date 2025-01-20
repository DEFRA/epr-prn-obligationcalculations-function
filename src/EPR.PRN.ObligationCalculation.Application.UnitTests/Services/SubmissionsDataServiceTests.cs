using EPR.PRN.ObligationCalculation.Application.Configs;
using EPR.PRN.ObligationCalculation.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;

namespace EPR.PRN.ObligationCalculation.Application.UnitTests.Services;

[TestClass]
public class SubmissionsDataServiceTests
{
    private Mock<ILogger<SubmissionsDataService>> _loggerMock = null!;
    private Mock<IOptions<SubmissionsServiceApiConfig>> _configMock = null!;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock = null!;
    private HttpClient _httpClient = null!;
    private SubmissionsDataService _service = null!;

    private readonly string _lastSuccessfulRunDate = "2023-09-01";

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<SubmissionsDataService>>();
        _configMock = new Mock<IOptions<SubmissionsServiceApiConfig>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        
        var config = new SubmissionsServiceApiConfig
        {
            BaseUrl = "https://api.example.com/",
            SubmissionsEndPoint = "submissions"
        };
        _configMock.Setup(c => c.Value).Returns(config);
        
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri(_configMock.Object.Value.BaseUrl)
        };

        _service = new SubmissionsDataService(_loggerMock.Object, _httpClient, _configMock.Object);
    }

    [TestMethod]
    public async Task GetSubmissions_ShouldReturnValidData_WhenApiResponseIsSuccessful()
    {
        // Arrange
        var expectedLogMessage = $"Get Approved Submissions Data from {_lastSuccessfulRunDate}";
        var organisationId = Guid.NewGuid();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    "[{ \"OrganisationId\": \"" + organisationId + "\" }]")
            });

        // Act
        var result = await _service.GetApprovedSubmissionsData(_lastSuccessfulRunDate);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result[0].OrganisationId.Should().Be(organisationId);

        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedLogMessage)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
    }

    [TestMethod]
    public async Task GetSubmissions_ShouldReturnEmptyList_WhenApiResponseIsEmptyString()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent,
                Content = new StringContent(string.Empty)
            });

        // Act
        var result = await _service.GetApprovedSubmissionsData(_lastSuccessfulRunDate);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(0);
    }

    [TestMethod]
    public async Task GetSubmissions_ShouldThrowException_WhenHttpClientThrowsException()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new Exception("Test Exception"));

        // Act & Assert
        _ = await Assert.ThrowsExceptionAsync<Exception>(() =>
            _service.GetApprovedSubmissionsData(_lastSuccessfulRunDate));

        // Assert handled by ExpectedException
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}