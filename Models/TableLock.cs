using System;

namespace RestauranteAPI.Models
{
    public class TableLock
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string Reason { get; set; } = string.Empty;

        // Relationships
        public int TableId { get; set; }
        public Table Table { get; set; } = null!;
    }
}
