namespace RestauranteAPI.DTOs
{
    public class MesaCreateDTO
    {
        public int Numero { get; set; }
        public int Capacidad { get; set; }
        public int ZonaId { get; set; }
    }

    public class MesaResponseDTO
    {
        public int Id { get; set; }
        public int Numero { get; set; }
        public int Capacidad { get; set; }
        public int ZonaId { get; set; }
    }
}
