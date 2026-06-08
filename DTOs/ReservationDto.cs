using System;

namespace RestauranteAPI.DTOs
{
    public class ReservationDto
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int Capacity { get; set; } // Maps to GuestCount in model
        public int StatusId { get; set; }
        public int ClientId { get; set; }
        public int TableId { get; set; }
        public int TurnId { get; set; }
    }
}
