namespace RestauranteAPI.Entidades
{
    public class Bloqueo
    {
        public int Id { get; set; }

        public DateOnly Fecha { get; set; }

        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
        public string Motivo{ get; set; } = string.Empty;

        public int MesaId { get; set; }
    }
}