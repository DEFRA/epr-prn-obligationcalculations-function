#nullable disable

using EPR.PRN.ObligationCalculation.Application.DTOs;
using EPR.PRN.ObligationCalculation.Application.Services;
using Moq;
using System.Diagnostics.CodeAnalysis;

namespace EPR.PRN.ObligationCalculation.Application.UnitTests;

[ExcludeFromCodeCoverage]
[TestClass]
public class ServiceBusProviderTests
{
    Mock<IServiceBusProvider> serviceBusProvider;
    List<ApprovedSubmissionEntity> approvedSubmissionEntities;

    [TestInitialize]
    public void TestInitialize()
    {
        approvedSubmissionEntities =
        [
            new() {
                SubmissionId = Guid.Empty,
                PackagingMaterial = "PC",
                PackagingMaterialWeight = 100,
                SubmissionPeriod = "2024 P1",
                OrganisationId = 1234
            },
            new() {
                SubmissionId = Guid.Empty,
                PackagingMaterial = "PL",
                PackagingMaterialWeight = 200,
                SubmissionPeriod = "2024 P2",
                OrganisationId = 1234
            }
        ];

        serviceBusProvider = new Mock<IServiceBusProvider>();
        serviceBusProvider.Setup(x => x.SendApprovedSubmissionsToQueue(approvedSubmissionEntities)).Returns(Task.CompletedTask);
    }

    [TestMethod]
    public async Task SendApprovedSubmissionsToQueue_WhenCalled_ShouldCallCreateSender()
    {
        // Arrange
        var service = serviceBusProvider.Object;

        // Act
        await service.SendApprovedSubmissionsToQueue(approvedSubmissionEntities);

        // Assert
        serviceBusProvider.Verify(x => x.SendApprovedSubmissionsToQueue(approvedSubmissionEntities), Times.Once);
    }
}
