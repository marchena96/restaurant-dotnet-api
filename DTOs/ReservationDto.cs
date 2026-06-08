namespace RestauranteAPI.DTOs
{
    public class ReservationDto
    {
        public int Id { get; set; }
        public string Date { get; set; } = string.Empty;
        public string ReservationTime { get; set; } = string.Empty;
        public int GuestCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public int TableId { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public string ZoneName { get; set; } = string.Empty;
        public int TurnId { get; set; }
        public string TurnName { get; set; } = string.Empty;
    }
}
