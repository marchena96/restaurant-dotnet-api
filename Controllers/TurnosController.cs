using Microsoft.AspNetCore.Mvc;
using RestauranteAPI.Entidades;
using RestauranteAPI.Servicios.Interfaces;

namespace RestauranteAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TurnosController : ControllerBase
    {
        private readonly ITurnoService _svTurno;

        public TurnosController(ITurnoService svTurno)
        {
            _svTurno = svTurno;
        }


        //LISTAR TURNOS
        [HttpGet]
        public IActionResult GetTurnos()
        {
            return Ok(_svTurno.GetTurnos());
        }

        //BUSCAR TURNO POR ID

        [HttpGet("{id}")]
        public IActionResult GetTurnoById(int id)
        {
            var turno = _svTurno.GetTurnoById(id);

            if (turno == null)
                return NotFound("No se encontró el turno.");

            return Ok(turno);
        }

        //CREAR TURNO

        [HttpPost]
        public IActionResult CrearTurno([FromBody] Turno turno)
        {
            var turnoCreado = _svTurno.CrearTurno(turno);
            return Ok(turnoCreado);
        }


        // ACTUALIZAR TURNO
        [HttpPut("{id}")]
        public IActionResult ActualizarTurno(int id, [FromBody] Turno turno)
        {
            var result = _svTurno.ActualizarTurno(id, turno);

            if (result == null)
                return NotFound("No se encontró el turno.");

            return Ok(result);
        }


        //ELIMINAR TURNO
        [HttpDelete("{id}")]
        public IActionResult EliminarTurno(int id)
        {
            _svTurno.EliminarTurno(id);
            return NoContent();
        }

        //CONSULTAR LAS HORAS DEL TURNO QUE ESTÉ CREADO.

        [HttpGet("validar/{horaInicio}/{horaFin}")]
        public IActionResult ValidarHora(TimeOnly horaInicio, TimeOnly horaFin)
        {
            var esValido = _svTurno.ValidarHora(horaInicio, horaFin);
            return Ok(esValido);
        }
    }
}
