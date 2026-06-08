namespace RestauranteAPI.DTOs
{
    public class ZoneSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TableCount { get; set; }
        public double OccupancyPercent { get; set; }
    }
}
