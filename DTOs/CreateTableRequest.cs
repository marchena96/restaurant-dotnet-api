namespace RestauranteAPI.DTOs
{
    public class CreateTableRequest
    {
        public string TableNumber { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int ZoneId { get; set; }
    }
}
