namespace EPR.PRN.ObligationCalculation.Application.Configs
{
    public class CommonDataApiConfig
    {
        public const string SectionName = "CommonDataApiConfig";
        public string BaseUrl { get; set; } = null!;
        public string Endpoint { get; set; } = null!;
        public int Timeout { get; set; }
        public int ServiceRetryCount { get; set; }
        public int ApiVersion { get; set; }
    }
}