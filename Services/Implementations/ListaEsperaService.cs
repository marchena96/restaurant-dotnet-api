using RestauranteAPI.Data;
using RestauranteAPI.DTOs;
using RestauranteAPI.Entidades;
using RestauranteAPI.Servicios.Interfaces;

namespace RestauranteAPI.Servicios.Implementaciones
{
    public class ListaEsperaService : IListaEsperaService
    {
        private readonly MyAppDbContext _db;

        public ListaEsperaService(MyAppDbContext db)
        {
            _db = db;
        }


        //obtener todas las listas de espera
        List<ListaEsperaResponseDTO> IListaEsperaService.GettAllListaEsperas()
        {
            return _db.ListaEspera
                .Select(listaespera => new ListaEsperaResponseDTO
                {
                    Id = listaespera.Id,
                    ClienteId = listaespera.ClienteId,
                    Fecha = listaespera.Fecha,
                    HoraInicio = listaespera.HoraInicio,
                    HoraFin = listaespera.HoraFin,
                    Cantidad = listaespera.Cantidad
                })
                .ToList();
        }


        //obtener una lista de espera por id
        ListaEsperaResponseDTO IListaEsperaService.GetListaEsperaById(int listaEsperaId)
        {
            var result = _db.ListaEspera.Find(listaEsperaId);
            if (result == null)
            {
                return null;
            }

            return new ListaEsperaResponseDTO
            {
                Id = result.Id,
                ClienteId = result.ClienteId,
                Fecha = result.Fecha,
                HoraInicio = result.HoraInicio,
                HoraFin = result.HoraFin,
                Cantidad = result.Cantidad
            };
        }

        //crear una nueva lista de espera
        public ListaEsperaResponseDTO CrearListaEspera(ListaEsperaCreateDTO listaEsperaDTO)
        {
            var result = new ListaEspera
            {
                ClienteId = listaEsperaDTO.ClienteId,
                Fecha = listaEsperaDTO.Fecha,
                HoraInicio = listaEsperaDTO.HoraInicio,
                HoraFin = listaEsperaDTO.HoraFin,
                Cantidad = listaEsperaDTO.Cantidad
            };

            _db.ListaEspera.Add(result);
            _db.SaveChanges();

            return new ListaEsperaResponseDTO
            {
                Id = result.Id,
                ClienteId = result.ClienteId,
                Fecha = result.Fecha,
                HoraInicio = result.HoraInicio,
                HoraFin = result.HoraFin,
                Cantidad = result.Cantidad
            };
        }
        //actualizar una lista de espera existente
        public ListaEsperaResponseDTO ActualizarListaEspera(int listaesperaId, ListaEsperaCreateDTO listaesperaDTO)
        {
            var result = _db.ListaEspera.Find(listaesperaId);

            if (result == null)
            {
                return null;
            }

            result.ClienteId = listaesperaDTO.ClienteId;
            result.Fecha = listaesperaDTO.Fecha;
            result.HoraInicio = listaesperaDTO.HoraInicio;
            result.HoraFin = listaesperaDTO.HoraFin;
            result.Cantidad = listaesperaDTO.Cantidad;

            _db.SaveChanges();

            return new ListaEsperaResponseDTO
            {
                Id = result.Id,
                ClienteId = result.ClienteId,
                Fecha = result.Fecha,
                HoraInicio = result.HoraInicio,
                HoraFin = result.HoraFin,
                Cantidad = result.Cantidad
            };

        }

        //Convierte una lista de espera en una reserva, asignándole una mesa disponible
        public void PromoverAReserva(int listaEsperaId, int mesaId)
        {
            var listaEspera = _db.ListaEspera.Find(listaEsperaId);

            if (listaEspera == null)
            {
                throw new Exception($"La lista de espera con id {listaEsperaId} no existe.");
            }

            var mesa = _db.Mesas.Find(mesaId);

            if (mesa == null)
            {
                throw new Exception($"La mesa con id {mesaId} no existe.");
            }

            if (listaEspera.HoraInicio >= listaEspera.HoraFin)
            {
                throw new Exception("La hora inicio debe ser menor que la hora fin.");
            }

            var zona = _db.Zonas.Find(mesa.ZonaId);

            if (zona == null || zona.Disponibilidad == false)
            {
                throw new Exception("La zona de la mesa no está activa.");
            }

            bool turnoValido = _db.Turnos.Any(turno =>
                turno.Activo &&
                listaEspera.HoraInicio >= turno.HoraInicio &&
                listaEspera.HoraFin <= turno.HoraFin
            );

            if (!turnoValido)
            {
                throw new Exception("La solicitud no está dentro de un turno válido.");
            }

            bool mesaBloqueada = _db.Bloqueos.Any(bloqueo =>
                bloqueo.MesaId == mesaId &&
                bloqueo.Fecha == listaEspera.Fecha &&
                listaEspera.HoraInicio < bloqueo.HoraFin &&
                listaEspera.HoraFin > bloqueo.HoraInicio
            );

            if (mesaBloqueada)
            {
                throw new Exception("La mesa tiene un bloqueo en ese horario.");
            }

            var estadoActivo = _db.Estados.FirstOrDefault(estado => estado.Nombre == "Activa");

            if (estadoActivo == null)
            {
                throw new Exception("No existe el estado Activa.");
            }

            bool mesaReservada = _db.Reservas.Any(reserva =>
                reserva.MesaId == mesaId &&
                reserva.Fecha == listaEspera.Fecha &&
                reserva.EstadoId == estadoActivo.Id &&
                listaEspera.HoraInicio < reserva.HoraFin &&
                listaEspera.HoraFin > reserva.HoraInicio
            );

            if (mesaReservada)
            {
                throw new Exception("La mesa ya tiene una reserva activa en ese horario.");
            }

            var reserva = new Reserva
            {
                ClienteId = listaEspera.ClienteId,
                Fecha = listaEspera.Fecha,
                HoraInicio = listaEspera.HoraInicio,
                HoraFin = listaEspera.HoraFin,
                Capacidad = listaEspera.Cantidad,
                MesaId = mesaId,
                EstadoId = estadoActivo.Id
            };

            _db.Reservas.Add(reserva);
            _db.ListaEspera.Remove(listaEspera);
            _db.SaveChanges();
        }
    }
}