using System;

namespace RestauranteAPI.DTOs
{
    public class WaitingListDto
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int PartySize { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string Status { get; set; } = "Waiting";
        public string ArrivedAt { get; set; } = string.Empty;
        public string? PreferredZone { get; set; }
    }
}
