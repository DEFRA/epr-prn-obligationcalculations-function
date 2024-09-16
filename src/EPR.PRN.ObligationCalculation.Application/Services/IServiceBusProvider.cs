using EPR.PRN.ObligationCalculation.Application.DTOs;

namespace EPR.PRN.ObligationCalculation.Application.Services
{
    public interface IServiceBusProvider
    {
        Task SendApprovedSubmissionsToQueue(List<ApprovedSubmissionEntity> approvedSubmissionEntities);
    }
}
