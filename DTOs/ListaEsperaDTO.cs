namespace RestauranteAPI.DTOs
{
   
    public class ListaEsperaCreateDTO
    {
        public int ClienteId { get; set; }
        public DateOnly Fecha { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
        public int Cantidad { get; set; }
    }

    public class ListaEsperaResponseDTO
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public DateOnly Fecha { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
        public int Cantidad { get; set; }
    
      
    }
}
