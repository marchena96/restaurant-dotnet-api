using System;

namespace RestauranteAPI.DTOs
{
    public class WaitingListDto
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int Quantity { get; set; } // Maps to PartySize in model
        public int ClientId { get; set; }
        public string Status { get; set; } = "Waiting";
    }
}
