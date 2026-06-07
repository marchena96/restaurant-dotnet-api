using RestauranteAPI.Data;
using RestauranteAPI.DTOs;
using RestauranteAPI.Entidades;
using RestauranteAPI.Servicios.Interfaces;

namespace RestauranteAPI.Servicios.Implementaciones
{
    public class ReservaService : IReservaService
    {
        private readonly MyAppDbContext _db;

        public ReservaService(MyAppDbContext db)
        {
            _db = db;
        }


        //obtener todas las reservas
        public List<ReservaResponseDTO> GetReservas()
        {
            return _db.Reservas
                .Select(reserva => new ReservaResponseDTO
                {
                    Id = reserva.Id,
                    Fecha = reserva.Fecha,
                    HoraInicio = reserva.HoraInicio,
                    HoraFin = reserva.HoraFin,
                    Capacidad = reserva.Capacidad,
                    ClienteId = reserva.ClienteId,
                    MesaId = reserva.MesaId,
                    EstadoId = reserva.EstadoId,
                    Estado = _db.Estados
                        .Where(estado => estado.Id == reserva.EstadoId)
                        .Select(estado => estado.Nombre)
                        .FirstOrDefault()
                })
                .ToList();
        }
        //obtener reserva por id
        public ReservaResponseDTO GetReservaById(int reservaId)
        {
            var reserva = _db.Reservas.Find(reservaId);

            if (reserva == null)
                return null;

            var estado = _db.Estados.Find(reserva.EstadoId);

            return new ReservaResponseDTO
            {
                Id = reserva.Id,
                Fecha = reserva.Fecha,
                HoraInicio = reserva.HoraInicio,
                HoraFin = reserva.HoraFin,
                Capacidad = reserva.Capacidad,
                ClienteId = reserva.ClienteId,
                MesaId = reserva.MesaId,
                EstadoId = reserva.EstadoId,
                Estado = estado.Nombre
            };
        }

        //crear reserva con validaciones

        public ReservaResponseDTO CrearReserva(ReservaCreateDTO reservaDTO)
        {
            if (reservaDTO.HoraInicio >= reservaDTO.HoraFin)
                throw new Exception("La hora inicio debe ser menor que la hora fin.");

            var cliente = _db.Clientes.Find(reservaDTO.ClienteId);
            if (cliente == null)
                throw new Exception("El cliente no existe.");

            var mesa = _db.Mesas.Find(reservaDTO.MesaId);
            if (mesa == null)
                throw new Exception("La mesa no existe.");

            //  cantidad de personas valida segun capacidad de mesa
            if (reservaDTO.Capacidad <= 0)
                throw new Exception("La cantidad de personas debe ser mayor a cero.");

            if (reservaDTO.Capacidad > mesa.Capacidad)
                throw new Exception("La cantidad de personas excede la capacidad de la mesa.");

            var zona = _db.Zonas.Find(mesa.ZonaId);
            if (zona == null || zona.Disponibilidad == false)
                throw new Exception("La zona de la mesa no está activa.");

            bool turnoValido = _db.Turnos.Any(turno =>
                turno.Activo &&
                reservaDTO.HoraInicio >= turno.HoraInicio &&
                reservaDTO.HoraFin <= turno.HoraFin
            );

            if (!turnoValido)
                throw new Exception("La reserva no está dentro de un turno válido.");

            bool mesaBloqueada = _db.Bloqueos.Any(bloqueo =>
                bloqueo.MesaId == reservaDTO.MesaId &&
                bloqueo.Fecha == reservaDTO.Fecha &&
                reservaDTO.HoraInicio < bloqueo.HoraFin &&
                reservaDTO.HoraFin > bloqueo.HoraInicio
            );

            if (mesaBloqueada)
            {
                MandarAListaEspera(reservaDTO);
                throw new Exception("La mesa tiene un bloqueo. El cliente fue enviado a lista de espera.");
            }

            var estadoActivo = _db.Estados
                .FirstOrDefault(estado => estado.Nombre == "Activa");

            var estadoCancelado = _db.Estados
                .FirstOrDefault(estado => estado.Nombre == "Cancelada");

            if (estadoActivo == null || estadoCancelado == null)
                throw new Exception("No existen los estados necesarios.");

            // Regla: una reserva cancelada no ocupa mesa
            bool mesaReservada = _db.Reservas.Any(reserva =>
                reserva.MesaId == reservaDTO.MesaId &&
                reserva.Fecha == reservaDTO.Fecha &&
                reserva.EstadoId != estadoCancelado.Id &&
                reservaDTO.HoraInicio < reserva.HoraFin &&
                reservaDTO.HoraFin > reserva.HoraInicio
            );

            if (mesaReservada)
            {
                MandarAListaEspera(reservaDTO);
                throw new Exception("La mesa ya está reservada en ese horario. El cliente fue enviado a lista de espera.");
            }

            var reserva = new Reserva
            {
                Fecha = reservaDTO.Fecha,
                HoraInicio = reservaDTO.HoraInicio,
                HoraFin = reservaDTO.HoraFin,
                Capacidad = reservaDTO.Capacidad,
                ClienteId = reservaDTO.ClienteId,
                MesaId = reservaDTO.MesaId,
                EstadoId = estadoActivo.Id
            };

            _db.Reservas.Add(reserva);
            _db.SaveChanges();

            return new ReservaResponseDTO
            {
                Id = reserva.Id,
                Fecha = reserva.Fecha,
                HoraInicio = reserva.HoraInicio,
                HoraFin = reserva.HoraFin,
                Capacidad = reserva.Capacidad,
                ClienteId = reserva.ClienteId,
                MesaId = reserva.MesaId,
                EstadoId = reserva.EstadoId,
                Estado = estadoActivo.Nombre
            };
        }

        //actualizar reserva con validaciones similares a crear reserva
        public ReservaResponseDTO ActualizarReserva(int reservaId, ReservaCreateDTO reservaDTO)
        {
            var reserva = _db.Reservas.Find(reservaId);

            if (reserva == null)
                return null;

            reserva.Fecha = reservaDTO.Fecha;
            reserva.HoraInicio = reservaDTO.HoraInicio;
            reserva.HoraFin = reservaDTO.HoraFin;
            reserva.Capacidad = reservaDTO.Capacidad;
            reserva.ClienteId = reservaDTO.ClienteId;
            reserva.MesaId = reservaDTO.MesaId;

            _db.SaveChanges();

            var estado = _db.Estados.Find(reserva.EstadoId);

            return new ReservaResponseDTO
            {
                Id = reserva.Id,
                Fecha = reserva.Fecha,
                HoraInicio = reserva.HoraInicio,
                HoraFin = reserva.HoraFin,
                Capacidad = reserva.Capacidad,
                ClienteId = reserva.ClienteId,
                MesaId = reserva.MesaId,
                EstadoId = reserva.EstadoId,
                Estado = estado.Nombre
            };
        }

        //eliminar reserva
        public void EliminarReserva(int reservaId)
        {
            var reserva = _db.Reservas.Find(reservaId);

            if (reserva == null)
                return;

            _db.Reservas.Remove(reserva);
            _db.SaveChanges();
        }


        // Método privado para mandar a lista de espera
        private void MandarAListaEspera(ReservaCreateDTO reservaDTO)
        {
            var listaEspera = new ListaEspera
            {
                ClienteId = reservaDTO.ClienteId,
                Fecha = reservaDTO.Fecha,
                HoraInicio = reservaDTO.HoraInicio,
                HoraFin = reservaDTO.HoraFin,
                Cantidad = reservaDTO.Capacidad
            };

            _db.ListaEspera.Add(listaEspera);
            _db.SaveChanges();
        }


        // Consultar reservas por cliente
        public List<ReservaResponseDTO> ConsultarReservasPorCliente(int clienteId)
        {
            return _db.Reservas
                .Where(reservas => reservas.ClienteId == clienteId)
                .Select(reservas => new ReservaResponseDTO
                {
                    Id = reservas.Id,
                    Fecha = reservas.Fecha,
                    HoraInicio = reservas.HoraInicio,
                    HoraFin = reservas.HoraFin,
                    Capacidad = reservas.Capacidad,
                    ClienteId = reservas.ClienteId,
                    MesaId = reservas.MesaId,
                    EstadoId = reservas.EstadoId,
                    Estado = _db.Estados
                        .Where(estados => estados.Id == reservas.EstadoId)
                        .Select(estados => estados.Nombre)
                        .FirstOrDefault()
                })
                .ToList();
        }

        // Cambiar el estado de una reserva
        public ReservaResponseDTO CambiarEstadoReserva(int reservaId, int estadoId)
        {
            var reserva = _db.Reservas.Find(reservaId);

            if (reserva == null)
                return null;

            var estado = _db.Estados.Find(estadoId);

            if (estado == null)
                throw new Exception("El estado no existe.");

            reserva.EstadoId = estadoId;
            _db.SaveChanges();

            return new ReservaResponseDTO
            {
                Id = reserva.Id,
                Fecha = reserva.Fecha,
                HoraInicio = reserva.HoraInicio,
                HoraFin = reserva.HoraFin,
                Capacidad = reserva.Capacidad,
                ClienteId = reserva.ClienteId,
                MesaId = reserva.MesaId,
                EstadoId = reserva.EstadoId,
                Estado = estado.Nombre
            };
        }

        // Consultar reservas por fecha

        public List<ReservaResponseDTO> ConsultarReservasPorFecha(DateOnly fecha)
        {
            return _db.Reservas
                .Where(reserva => reserva.Fecha == fecha)
                .Select(reserva => new ReservaResponseDTO
                {
                    Id = reserva.Id,
                    Fecha = reserva.Fecha,
                    HoraInicio = reserva.HoraInicio,
                    HoraFin = reserva.HoraFin,
                    Capacidad = reserva.Capacidad,
                    ClienteId = reserva.ClienteId,
                    MesaId = reserva.MesaId,
                    EstadoId = reserva.EstadoId,
                    Estado = _db.Estados
                        .Where(estado => estado.Id == reserva.EstadoId)
                        .Select(estado => estado.Nombre)
                        .FirstOrDefault()
                })
                .ToList();
        }

        // Obtener historial de reservas de un cliente
        public List<ReservaResponseDTO> ObtenerHistorialReservasCliente(int clienteId)
        {
            var cliente = _db.Clientes.Find(clienteId);

            if (cliente == null)
            {
                throw new Exception("El cliente no existe.");
            }

            return _db.Reservas
                .Where(reserva => reserva.ClienteId == clienteId)
                .Select(reserva => new ReservaResponseDTO
                {
                    Id = reserva.Id,
                    Fecha = reserva.Fecha,
                    HoraInicio = reserva.HoraInicio,
                    HoraFin = reserva.HoraFin,
                    Capacidad = reserva.Capacidad,
                    ClienteId = reserva.ClienteId,
                    MesaId = reserva.MesaId,
                    EstadoId = reserva.EstadoId,
                    Estado = _db.Estados
                        .Where(e => e.Id == reserva.EstadoId)
                        .Select(e => e.Nombre)
                        .FirstOrDefault()
                })
                .ToList();
        }


        // Verificar el estado de una reserva
        public bool VerificarEstadoReserva(int reservaId, string estadoNombre)
        {
            var reserva = _db.Reservas.Find(reservaId);

            if (reserva == null)
            {
                throw new Exception("La reserva no existe.");
            }

            var estado = _db.Estados.Find(reserva.EstadoId);

            if (estado == null)
            {
                throw new Exception("El estado de la reserva no existe.");
            }

            return estado.Nombre.ToLower() == estadoNombre.ToLower();
        }
    }
    }
    

