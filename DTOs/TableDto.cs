namespace RestauranteAPI.DTOs
{
    public class TableDto
    {
        public int Id { get; set; }
        public string Number { get; set; } = string.Empty; // Maps to TableNumber in model
        public int Capacity { get; set; }
        public int ZoneId { get; set; }
    }
}
