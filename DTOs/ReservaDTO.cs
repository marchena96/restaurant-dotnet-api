namespace RestauranteAPI.DTOs
{
    
    public class ReservaCreateDTO
    {
        public DateOnly Fecha { get; set; }        
        public TimeOnly HoraInicio { get; set; }   
        public TimeOnly HoraFin { get; set; }      
        public int Capacidad { get; set; }         
        public int ClienteId { get; set; }         
        public int MesaId { get; set; }

        public int EstadoId { get; set; }
    }

 
    public class ReservaResponseDTO
    {
        public int Id { get; set; }                
        public DateOnly Fecha { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
        public int Capacidad { get; set; }
        public int ClienteId { get; set; }
        public int EstadoId { get; set; }
        public string Estado { get; set; }
        public int MesaId { get;  set; }
    }
}
