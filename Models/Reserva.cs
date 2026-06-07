namespace RestauranteAPI.Entidades
{
    public class Reserva
    {
        public int Id { get; set; }

        public DateOnly Fecha { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }

        public int Capacidad { get; set; }

        //relaciones
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; }

        public int MesaId { get; set; }
        public Mesa Mesa { get; set; }

        public int EstadoId { get; set; }
        public Estado Estado { get; set; }
    }
}