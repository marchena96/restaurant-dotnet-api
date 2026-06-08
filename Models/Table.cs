using System.Collections.Generic;

namespace RestauranteAPI.Models
{
    public class Table
    {
        public int Id { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public int Capacity { get; set; }

        // Relationships
        public int ZoneId { get; set; }
        public Zone Zone { get; set; } = null!;

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public ICollection<TableLock> TableLocks { get; set; } = new List<TableLock>();
    }
}
