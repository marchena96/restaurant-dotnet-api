using RestauranteAPI.Data;
using RestauranteAPI.DTOs;
using RestauranteAPI.Entidades;
using RestauranteAPI.Servicios.Interfaces;


namespace RestauranteAPI.Servicios.Implementaciones
{
    public class BloqueoService : IBloqueoService
    {
        private readonly MyAppDbContext _db;

        public BloqueoService(MyAppDbContext db)
        {
            _db = db;
        }


        //obtener todos los bloqueos
        public List<BloqueoResponseDTO> GetAllBloqueos()
        {
            return _db.Bloqueos.Select(bloqueo => new BloqueoResponseDTO
            {
                Id = bloqueo.Id,
                MesaId = bloqueo.MesaId,
                Fecha = bloqueo.Fecha,
                HoraInicio = bloqueo.HoraInicio,
                HoraFin = bloqueo.HoraFin,
                Motivo = bloqueo.Motivo
            }).ToList();
        }

        //obtener bloqueo por id
        public BloqueoResponseDTO GetBloqueoById(int bloqueoId)
        {
            var result = _db.Bloqueos.Find(bloqueoId);
            if (result == null)
            {
                return null;
            }

            return new BloqueoResponseDTO
            {
                Id = result.Id,
                MesaId = result.MesaId,
                Fecha = result.Fecha,
                HoraInicio = result.HoraInicio,
                HoraFin = result.HoraFin,
                Motivo = result.Motivo
            };
        }


        //actualizar bloqueo
        public BloqueoResponseDTO ActualizarBloqueo(int bloqueoId, BloqueoResponseDTO bloqueoDTO) { 
            var result = _db.Bloqueos.Find(bloqueoId); 
            if (result == null) { 
                return null; } 
            result.Fecha = bloqueoDTO.Fecha; 
            result.HoraInicio = bloqueoDTO.HoraInicio; 
            result.HoraFin = bloqueoDTO.HoraFin; 
            result.Motivo = bloqueoDTO.Motivo; 
            result.MesaId = bloqueoDTO.MesaId; 
            _db.SaveChanges(); 
            return new BloqueoResponseDTO 
            { Id = result.Id, 
                MesaId = result.MesaId, 
                Fecha = result.Fecha, 
                HoraInicio = result.HoraInicio, 
                HoraFin = result.HoraFin, 
                Motivo = result.Motivo }; }



        //desbloquear mesa(eliminar un bloqueo)


        public void DesbloquearMesa(int bloqueoId)
        {
            var bloqueo = _db.Bloqueos.Find(bloqueoId);

            if (bloqueo == null)
            {
                throw new Exception($"No existe un bloqueo con id {bloqueoId}.");
            }

            _db.Bloqueos.Remove(bloqueo);
            _db.SaveChanges();
        }

        //bloquear mesa(como crear un objeto de bloqueo)

        public BloqueoResponseDTO BloquearMesa(BloqueoCreateDTO bloqueoDTO)
        {
            var mesa = _db.Mesas.Find(bloqueoDTO.MesaId);

            if (mesa == null)
                throw new Exception("La mesa no existe.");

            if (bloqueoDTO.HoraInicio >= bloqueoDTO.HoraFin)
                throw new Exception("La hora inicio debe ser menor que la hora fin.");

            var existeBloqueo = _db.Bloqueos.Any(bloqueo =>
                bloqueo.MesaId == bloqueoDTO.MesaId &&
                bloqueo.Fecha == bloqueoDTO.Fecha &&
                bloqueoDTO.HoraInicio < bloqueo.HoraFin &&
                bloqueoDTO.HoraFin > bloqueo.HoraInicio
            );

            if (existeBloqueo)
                throw new Exception("La mesa ya tiene un bloqueo en ese horario.");

            var bloqueo = new Bloqueo
            {
                MesaId = bloqueoDTO.MesaId,
                Fecha = bloqueoDTO.Fecha,
                HoraInicio = bloqueoDTO.HoraInicio,
                HoraFin = bloqueoDTO.HoraFin,
                Motivo = bloqueoDTO.Motivo
            };

            _db.Bloqueos.Add(bloqueo);
            _db.SaveChanges();

            return new BloqueoResponseDTO
            {
                Id = bloqueo.Id,
                MesaId = bloqueo.MesaId,
                Fecha = bloqueo.Fecha,
                HoraInicio = bloqueo.HoraInicio,
                HoraFin = bloqueo.HoraFin,
                Motivo = bloqueo.Motivo
            };
        }
    }
    }

