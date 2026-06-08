namespace RestauranteAPI.DTOs
{
    public class TableDto
    {
        public int Id { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ZoneName { get; set; } = string.Empty;
    }
}
