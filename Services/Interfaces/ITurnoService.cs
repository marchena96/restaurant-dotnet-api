using RestauranteAPI.Entidades;

namespace RestauranteAPI.Servicios.Interfaces
{
    public interface ITurnoService
    {
        public List<Turno> GetTurnos();
        public Turno GetTurnoById(int turnoId);
        public Turno CrearTurno(Turno turno);
        public Turno ActualizarTurno(int turnoId, Turno turno);
        public void EliminarTurno(int turnoId);
        public bool ValidarHora(TimeOnly horaInicio, TimeOnly horaFin);
    }
}
