namespace RestauranteAPI.DTOs
{
   
    public class BloqueoCreateDTO
    {
        public int MesaId { get; set; }          
        public DateOnly Fecha { get; set; }     
        public TimeOnly HoraInicio { get; set; } 
        public TimeOnly HoraFin { get; set; }    
        public string Motivo { get; set; }       
    }

   
    public class BloqueoResponseDTO
    {
        public int Id { get; set; }
        public int MesaId { get; set; }
        public DateOnly Fecha { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
        public string Motivo { get; set; }

    }
}
