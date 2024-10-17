namespace EPR.PRN.ObligationCalculation.Application.Services;

public interface IAppInsightsProvider
{
    Task<DateTime> GetParameterForApprovedSubmissionsApiCall();
}