
using Microsoft.AspNetCore.Mvc;
using RestauranteAPI.DTOs;
using RestauranteAPI.Entidades;
using RestauranteAPI.Servicios.Interfaces;

namespace RestauranteAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ZonasController : ControllerBase
    {
        private readonly IZonaService _zonaService;

        public ZonasController(IZonaService zonaService)
        {
            _zonaService = zonaService;
        }

        // Obtener todos los clientes

        [HttpGet]
        public ActionResult<List<ZonaResponseDTO>> GetClientes()
        {
            return Ok(_zonaService.GetAllZonas());
        }
        // Obtener los clientes por id
        [HttpGet("{Nombre}")]
        public ActionResult<ZonaResponseDTO> GetZonaByNombre(string Nombre)
        {
            var zona = _zonaService.GetZonaByNombre(Nombre);

            if (zona == null)
            {
                return NotFound("No se encontró la zona.");
            }

            return Ok(zona);
        }


        // Crear una zona
        [HttpPost]
        public ActionResult<ZonaResponseDTO> CrearZona([FromBody] ZonaCreateDTO zonaDTO)
        {
            var zonaCreado = _zonaService.CrearZona(zonaDTO);
            return Ok(zonaCreado);
        }

        // Actualizar zona
        [HttpPut("{id}")]
        public ActionResult<ZonaResponseDTO> ActualizarZona(int id, [FromBody] ZonaCreateDTO zonaDTO)
        {
            var zonaActualizado = _zonaService.ActualizarZona(id, zonaDTO);

            if (zonaActualizado == null)
            {
                return NotFound("No se encontró la zona.");
            }

            return Ok(zonaActualizado);
        }


        // Eliminar zona
        [HttpDelete("{id}")]
        public IActionResult EliminarZona(int id)
        {
            _zonaService.EliminarZona(id);
            return NoContent();
        }
    }
}