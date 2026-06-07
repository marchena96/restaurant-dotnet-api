using Microsoft.AspNetCore.Mvc;
using RestauranteAPI.Entidades;
using RestauranteAPI.Servicios.Interfaces;

namespace RestauranteAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EstadosController : ControllerBase
    {
        private readonly IEstadoService _svEstado;

        public EstadosController(IEstadoService svEstado)
        {
            _svEstado = svEstado;
        }
        //ver todos los estados
        [HttpGet]
        public IActionResult GetEstados()
        {
            return Ok(_svEstado.GetEstados());
        }
        //buscar estado por id
        [HttpGet("{id}")]
        public IActionResult GetEstadoById(int id)
        {
            var estado = _svEstado.GetEstadoById(id);

            if (estado == null)
                return NotFound("No se encontró el estado.");

            return Ok(estado);
        }
        //crear estado
        [HttpPost]
        public IActionResult CrearEstado([FromBody] Estado estado)
        {
            var estadoCreado = _svEstado.CrearEstado(estado);
            return Ok(estadoCreado);
        }

        //actualizar estado

        [HttpPut("{id}")]
        public IActionResult ActualizarEstado(int id, [FromBody] Estado estado)
        {
            var result = _svEstado.ActualizarEstado(id, estado);

            if (result == null)
                return NotFound("No se encontró el estado.");

            return Ok(result);
        }
        //eliminar estado

        [HttpDelete("{id}")]
        public IActionResult EliminarEstado(int id)
        {
            _svEstado.EliminarEstado(id);
            return NoContent();
        }
    }
}
