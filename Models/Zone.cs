using System.Collections.Generic;

namespace RestauranteAPI.Models
{
    public class Zone
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }

        // Relationships
        public ICollection<Table> Tables { get; set; } = new List<Table>();
    }
}
