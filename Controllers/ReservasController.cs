using Microsoft.AspNetCore.Mvc;
using RestauranteAPI.DTOs;
using RestauranteAPI.Servicios.Implementaciones;
using RestauranteAPI.Servicios.Interfaces;

namespace RestauranteAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservasController : ControllerBase
    {
        private readonly IReservaService _svReserva;

        public ReservasController(IReservaService svReserva)
        {
            _svReserva = svReserva;
        }
        //OBTENER TODAS LAS RESERVAS

        [HttpGet]
        public IActionResult GetReservas()
        {
            var reservas = _svReserva.GetReservas();
            return Ok(reservas);
        }

        //BUSCAR RESERVA POR ID

        [HttpGet("{id:int}")]
        public IActionResult GetReservaById(int id)
        {
            var reserva = _svReserva.GetReservaById(id);

            if (reserva == null)
                return NotFound("Reserva no encontrada.");

            return Ok(reserva);
        }

        //CREAR RESERVA

        [HttpPost]
        public IActionResult CrearReserva([FromBody] ReservaCreateDTO reservaDTO)
        {
            try
            {
                var reserva = _svReserva.CrearReserva(reservaDTO);
                return Ok(reserva);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //ACTUALIZAR RESERVA

        [HttpPut("{id}")]
        public IActionResult ActualizarReserva(int id, [FromBody] ReservaCreateDTO reservaDTO)
        {
            try
            {
                var reserva = _svReserva.ActualizarReserva(id, reservaDTO);

                if (reserva == null)
                    return NotFound("Reserva no encontrada.");

                return Ok(reserva);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //ELIMINAR RESERVAS

        [HttpDelete("{id}")]
        public IActionResult EliminarReserva(int id)
        {
            _svReserva.EliminarReserva(id);

            return Ok("Reserva eliminada correctamente.");
        }

        // consultar reservas por cliente
        [HttpGet("cliente/{clienteId:int}")]
        public IActionResult ConsultarReservasPorCliente(int clienteId)
        {
            var reservas = _svReserva.ConsultarReservasPorCliente(clienteId);
            if (reservas == null || !reservas.Any())
                return NotFound("No se encontraron reservas para este cliente.");
            return Ok(reservas);
        }


        //CAMBIAR ESTADO DE RESERVA

        [HttpPut("{id}/estado/{estadoId}")]
        public IActionResult CambiarEstado(int Id, int estadoId)
        {
            try
            {
                var reserva = _svReserva.CambiarEstadoReserva(Id, estadoId);
                if (reserva == null)
                    return NotFound("Reserva no encontrada.");

                return Ok(reserva);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //BUSCAR RESERVAS EN RANGO DE FECHAS
        [HttpGet("fecha/{fecha}")]
        public IActionResult GetReservasPorFecha(DateOnly fecha)
        {
            try
            {
                var reservas = _svReserva.ConsultarReservasPorFecha(fecha);
                return Ok(reservas);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //obtener historial de reservas de un cliente

        [HttpGet("cliente/{clienteId:int}/historial")]
        public IActionResult ObtenerHistorialReservasCliente(int clienteId)
        {
            try
            {
                var historial = _svReserva.ObtenerHistorialReservasCliente(clienteId);
                return Ok(historial);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //CONSULTAR EL ESTADO DE UNA RESERVA

        [HttpGet("{id:int}/verificar-estado/{estado}")]
        public IActionResult VerificarEstadoReserva(int id, string estado)
        {
            try
            {
                var resultado = _svReserva.VerificarEstadoReserva(id, estado);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
    }