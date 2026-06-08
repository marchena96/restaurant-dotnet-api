using System.Collections.Generic;

namespace RestauranteAPI.Models
{
    public class Client
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string IdCard { get; set; } = string.Empty;

        // Relationships
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public ICollection<WaitingListEntry> WaitingListEntries { get; set; } = new List<WaitingListEntry>();
    }
}
