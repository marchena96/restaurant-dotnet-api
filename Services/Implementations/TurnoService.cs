using RestauranteAPI.Data;
using RestauranteAPI.Entidades;
using RestauranteAPI.Servicios.Interfaces;

namespace RestauranteAPI.Servicios.Implementaciones
{
    public class TurnoService : ITurnoService
    {
        private readonly MyAppDbContext _db;

        public TurnoService(MyAppDbContext db)
        {
            _db = db;
        }
        //obtener todos los turnos
        public List<Turno> GetTurnos()
        {
            return _db.Turnos.ToList();
        }
        //obtener un turno por id
        public Turno GetTurnoById(int turnoId)
        {
            return _db.Turnos.Find(turnoId);
        }

        //crear un nuevo turno
        public Turno CrearTurno(Turno turno)
        {
            var result = new Turno
            {
                Nombre = turno.Nombre,
                HoraInicio = turno.HoraInicio,
                HoraFin = turno.HoraFin,
                Activo = turno.Activo
            };

            _db.Turnos.Add(result);
            _db.SaveChanges();

            return result;
        }

        //actualizar un turno existente
        public Turno ActualizarTurno(int turnoId, Turno turno)
        {
            var result = _db.Turnos.Find(turnoId);

            if (result == null)
                return null;

            result.Nombre = turno.Nombre;
            result.HoraInicio = turno.HoraInicio;
            result.HoraFin = turno.HoraFin;
            result.Activo = turno.Activo;

            _db.SaveChanges();

            return result;
        }


        //eliminar un turno por id
        public void EliminarTurno(int turnoId)
        {
            var result = _db.Turnos.Find(turnoId);

            if (result == null)
                return;

            _db.Turnos.Remove(result);
            _db.SaveChanges();
        }

        //validar que la hora de inicio sea menor a la hora de fin y que el turno esté activo

        public bool ValidarHora(TimeOnly horaInicio, TimeOnly horaFin)
        {
            if (horaInicio >= horaFin)
                return false;

            return _db.Turnos.Any(turno =>
                turno.Activo &&
                horaInicio >= turno.HoraInicio &&
                horaFin <= turno.HoraFin
            );
        }
    }
}
