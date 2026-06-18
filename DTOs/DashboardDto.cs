using System.Collections.Generic;

namespace RestauranteAPI.DTOs
{
    public class DashboardResponse
    {
        public MetricsDto Metrics { get; set; } = new();
        public List<ZoneSummaryDto> Zones { get; set; } = [];
        public List<UpcomingBlockDto> UpcomingBlocks { get; set; } = [];
    }

    public class MetricsDto
    {
        public int ActiveReservations { get; set; }
        public int PendingReservations { get; set; }
        public int AvailableTables { get; set; }
        public int LargeTablesAvailable { get; set; }
        public int WaitingListCount { get; set; }
        public double AverageWaitMinutes { get; set; }
        public double OccupancyPercent { get; set; }
    }

    public class UpcomingBlockDto
    {
        public string Time { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string TableNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
