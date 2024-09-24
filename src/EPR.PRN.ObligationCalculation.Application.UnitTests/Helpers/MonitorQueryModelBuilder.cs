using Azure.Monitor.Query.Models;

namespace EPR.PRN.ObligationCalculation.Application.UnitTests.Helpers
{
    public static class MonitorQueryModelBuilder
    {
        public static LogsQueryResult CreateMockLogsQueryResult(DateTime? timeGenerated)
        {
            var columns = new List<LogsTableColumn>
            {
                MonitorQueryModelFactory.LogsTableColumn("TimeGenerated", LogsColumnType.String),
                MonitorQueryModelFactory.LogsTableColumn("Message", LogsColumnType.String)
            };
            var rows = new List<LogsTableRow>();
            if (timeGenerated != null)
            {
                rows =
                [
                    MonitorQueryModelFactory.LogsTableRow(columns, [timeGenerated.Value.ToString("o"), "Mock log message 1"]),
                ];
            }

            LogsTable logsTable = MonitorQueryModelFactory.LogsTable("PrimaryResult", columns.AsEnumerable(), rows.AsEnumerable());
            var queryResult = MonitorQueryModelFactory.LogsQueryResult([logsTable], new BinaryData("{}"), new BinaryData("{}"), new BinaryData("{}"));
            return queryResult;
        }
    }
}
