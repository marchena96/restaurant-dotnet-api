using System;
using System.Collections.Generic;

namespace RestauranteAPI.Models
{
    public class Turn
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public bool IsActive { get; set; }

        // Relationships
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
