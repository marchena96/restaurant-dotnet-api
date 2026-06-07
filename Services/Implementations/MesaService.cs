using RestauranteAPI.Data;
using RestauranteAPI.Entidades;
using RestauranteAPI.Servicios.Interfaces;
using RestauranteAPI.DTOs;

namespace RestauranteAPI.Servicios.Implementaciones
{
    public class MesaService : IMesaService
    {
        private readonly MyAppDbContext _db;

        public MesaService(MyAppDbContext db)
        {
            _db = db;
        }


        //obtener todas las mesas
        public List<MesaResponseDTO> GetAllMesas()
        {
            return _db.Mesas.
                Select(mesas => new MesaResponseDTO
            {
                Id = mesas.Id,
                Numero = mesas.Numero,
                Capacidad = mesas.Capacidad,
                ZonaId = mesas.ZonaId
            }).ToList();
        }
        //obtener mesa por id
        public MesaResponseDTO GetMesaById(int mesaId)
        {
            var mesa = _db.Mesas.Find(mesaId);
            if (mesa == null) return null;

            return new MesaResponseDTO
            {
                Id = mesa.Id,
                Numero = mesa.Numero,
                Capacidad = mesa.Capacidad,
                ZonaId = mesa.ZonaId
            };
        }


        //crear mesa
        public MesaResponseDTO CrearMesa(MesaCreateDTO mesaDTO)
        {
            var zona = _db.Zonas.Find(mesaDTO.ZonaId);

            if (zona == null)
                throw new ArgumentException("La zona indicada no existe.");

            if (zona.Disponibilidad == false)
                throw new InvalidOperationException("La zona de la mesa no está activa.");


            var mesa = new Mesa
                {
                    Numero = mesaDTO.Numero,
                    Capacidad = mesaDTO.Capacidad,
                    ZonaId = mesaDTO.ZonaId
                };
                _db.Mesas.Add(mesa);
                _db.SaveChanges();
                return new MesaResponseDTO
                {
                    Id = mesa.Id,
                    Numero = mesa.Numero,
                    Capacidad = mesa.Capacidad,
                    ZonaId = mesa.ZonaId
                };
            
        }

        //actualizar mesa
        public MesaResponseDTO ActualizarMesa(int mesaId, MesaCreateDTO mesaDTO)
        {
            var result = _db.Mesas.Find(mesaId);

            if (result == null) return null;

            result.Numero = mesaDTO.Numero;
            result.Capacidad = mesaDTO.Capacidad;
            result.ZonaId = mesaDTO.ZonaId;

            _db.Update(result);
            _db.SaveChanges();
            return new MesaResponseDTO
            {
                Id = result.Id,
                Numero = result.Numero,
                Capacidad = result.Capacidad,
                ZonaId = result.ZonaId
            };
        }


        //eliminar mesa
        public void EliminarMesa(int mesaId)
        {
            var result = _db.Mesas.Find(mesaId);
            if (result == null) return;

            _db.Mesas.Remove(result);
            _db.SaveChanges();
        }


        //obtener mesas disponibles para una fecha y hora específica
        public List<MesaResponseDTO> GetMesasDisponibles(DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin)
        {
            var mesasReservadas = _db.Reservas
                .Where(reserva => reserva.Fecha == fecha &&
                            reserva.HoraInicio < horaFin &&
                            reserva.HoraFin > horaInicio)
                .Select(reserva => reserva.MesaId)
                .Distinct()
                .ToList();

            var mesasBloquedas = _db.Bloqueos
                .Where(bloqueo => bloqueo.Fecha == fecha &&
                            bloqueo.HoraInicio < horaFin &&
                            bloqueo.HoraFin > horaInicio)
                .Select(bloqueo => bloqueo.MesaId)
                .Distinct()
                .ToList();

            return _db.Mesas
                .Where(mesa => !mesasReservadas.Contains(mesa.Id) &&
                            !mesasBloquedas.Contains(mesa.Id))
                .Select(mesa => new MesaResponseDTO
                {
                    Id = mesa.Id,
                    Numero = mesa.Numero,
                    Capacidad = mesa.Capacidad,
                    ZonaId = mesa.ZonaId
                })
                .ToList();
        }
        //validar si una mesa está disponible para una fecha y hora específica
        public bool ValidarDisponibilidadMesa(int mesaId, DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin)
        {
            var mesaExiste = _db.Mesas.Find(mesaId);
            if (mesaExiste == null) return false;

            bool tieneReserva = _db.Reservas.Any(reserva =>
                reserva.MesaId == mesaId &&
                reserva.Fecha == fecha &&
                reserva.HoraInicio < horaFin &&
                reserva.HoraFin > horaInicio);

            bool tieneBloqueo = _db.Bloqueos.Any(bloqueo =>
                bloqueo.MesaId == mesaId &&
                bloqueo.Fecha == fecha &&
                bloqueo.HoraInicio < horaFin &&
                bloqueo.HoraFin > horaInicio);

            return !tieneReserva && !tieneBloqueo;
        }

           public MesaResponseDTO BuscarMesaPorNumero(int numeroMesa)
        {
            var mesa = _db.Mesas.FirstOrDefault(m => m.Numero == numeroMesa);
            if (mesa == null) return null;
            return new MesaResponseDTO
            {
                Id = mesa.Id,
                Numero = mesa.Numero,
                Capacidad = mesa.Capacidad,
                ZonaId = mesa.ZonaId
            };
        }


        //buscar mesas por capacidad mínima
        public List<MesaResponseDTO> BuscarMesaPorCapacidad(int capacidad)
        {
            return _db.Mesas
                .Where(mesa => mesa.Capacidad >= capacidad)
                .Select(mesa => new MesaResponseDTO
                {
                    Id = mesa.Id,
                    Numero = mesa.Numero,
                    Capacidad = mesa.Capacidad,
                    ZonaId = mesa.ZonaId
                })
                .ToList();
        }
    }
    }
    
