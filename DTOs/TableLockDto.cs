using System;

namespace RestauranteAPI.DTOs
{
    public class TableLockDto
    {
        public int Id { get; set; }
        public int TableId { get; set; }
        public DateOnly Date { get; set; }
        public int TurnId { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
