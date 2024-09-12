namespace EPR.PRN.ObligationCalculation.Application.Services;

public interface ICommonDataService
{
    Task<HttpResponseMessage> GetApprovedSubmissionsWithAggregatedPomData(string approvedAfterDateString);
}