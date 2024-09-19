namespace EPR.PRN.ObligationCalculation.Application.Configs
{
    public class ServiceBusConfig
    {
        public const string SectionName = "ServiceBus";
        public string Namespace { get; set; } = null!;
        public string QueueName { get; set; } = null!;
        public string ConnectionString { get; set; } = null!;
    }
}
