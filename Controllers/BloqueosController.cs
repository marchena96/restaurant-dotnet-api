using Microsoft.AspNetCore.Mvc;
using RestauranteAPI.DTOs;
using RestauranteAPI.Servicios.Interfaces;

namespace RestauranteAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BloqueosController : ControllerBase
    {
        private readonly IBloqueoService _bloqueoService;

        public BloqueosController(IBloqueoService bloqueoService)
        {
            _bloqueoService = bloqueoService;
        }

        // GET: api/bloqueos
        [HttpGet]
        public ActionResult<List<BloqueoResponseDTO>> GetBloqueos()
        {
            return Ok(_bloqueoService.GetAllBloqueos());
        }

        // GET: api/bloqueos/5
        [HttpGet("{id}")]
        public ActionResult<BloqueoResponseDTO> GetBloqueoById(int id)
        {
            var bloqueo = _bloqueoService.GetBloqueoById(id);

            if (bloqueo == null)
                return NotFound("No se encontró el bloqueo.");

            return Ok(bloqueo);
        }

        // PUT: api/bloqueos/5
        [HttpPut("{id}")]
        public ActionResult<BloqueoResponseDTO> ActualizarBloqueo(int id, [FromBody] BloqueoCreateDTO bloqueoDTO)
        {
            var bloqueoactualizado = _bloqueoService.ActualizarBloqueo(id, new BloqueoResponseDTO
            {
                Id = id,
                MesaId = bloqueoDTO.MesaId,
                Fecha = bloqueoDTO.Fecha,
                HoraInicio = bloqueoDTO.HoraInicio,
                HoraFin = bloqueoDTO.HoraFin,
                Motivo = bloqueoDTO.Motivo
            });

            if (bloqueoactualizado == null)
                return NotFound("No se encontró el bloqueo.");

            return Ok(bloqueoactualizado);
        }


        // POST: api/bloqueos/bloquear
        [HttpPost("bloquear")]
        public IActionResult BloquearMesa([FromBody] BloqueoCreateDTO bloqueoDTO)
        {
            try
            {
                _bloqueoService.BloquearMesa(bloqueoDTO);
                return Ok("Mesa bloqueada correctamente");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/bloqueos/mesa/2
        [HttpDelete("mesa/{mesaId:int}")]
        public IActionResult DesbloquearMesa(int mesaId)
        {
            try
            {
                _bloqueoService.DesbloquearMesa(mesaId);
                return Ok("Bloqueos de la mesa eliminados correctamente.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}