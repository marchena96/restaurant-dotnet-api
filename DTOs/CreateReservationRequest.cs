namespace RestauranteAPI.DTOs
{
    public class CreateReservationRequest
    {
        public int ClientId { get; set; }
        public int TableId { get; set; }
        public string Date { get; set; } = string.Empty;
        public string ReservationTime { get; set; } = string.Empty;
        public int GuestCount { get; set; }
    }
}
