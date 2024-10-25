using EPR.PRN.ObligationCalculation.Application.DTOs;

namespace EPR.PRN.ObligationCalculation.Application.Services;

public interface IServiceBusProvider
{
    Task SendApprovedSubmissionsToQueueAsync(List<ApprovedSubmissionEntity> approvedSubmissionEntities);
    
    Task<string?> GetLastSuccessfulRunDateFromQueue();

    Task SendSuccessfulRunDateToQueue(string runDate);
}
