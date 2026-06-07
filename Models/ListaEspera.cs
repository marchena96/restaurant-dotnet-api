namespace RestauranteAPI.Entidades
{
    public class ListaEspera
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public DateOnly Fecha { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
        public int Cantidad { get; set; }
        
    }
}
