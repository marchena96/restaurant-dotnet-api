using System;

namespace RestauranteAPI.Models
{
    public class Reservation
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int GuestCount { get; set; }

        // Relationships
        public int ClientId { get; set; }
        public Client Client { get; set; } = null!;

        public int TableId { get; set; }
        public Table Table { get; set; } = null!;

        public int StatusId { get; set; }
        public Status Status { get; set; } = null!;

        public int TurnId { get; set; }
        public Turn Turn { get; set; } = null!;
    }
}
