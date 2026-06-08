using System.Collections.Generic;

namespace RestauranteAPI.Models
{
    public class Status
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Relationships
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
