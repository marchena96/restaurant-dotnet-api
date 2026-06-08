using System;

namespace RestauranteAPI.Models
{
    public class WaitingListEntry
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int PartySize { get; set; }
        public string Status { get; set; } = "Waiting"; // Waiting, Assigned, Cancelled
        public string? PreferredZone { get; set; }

        // Relationships
        public int ClientId { get; set; }
        public Client Client { get; set; } = null!;
    }
}
