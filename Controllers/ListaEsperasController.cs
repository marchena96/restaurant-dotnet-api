using Microsoft.AspNetCore.Mvc;
using RestauranteAPI.DTOs;
using RestauranteAPI.Entidades;
using RestauranteAPI.Servicios.Interfaces;



namespace RestauranteAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListaEsperaController : ControllerBase
    {
        private readonly IListaEsperaService _listaEsperaService;

        public ListaEsperaController(IListaEsperaService listaEsperaService)
        {
            _listaEsperaService = listaEsperaService;
        }

        //LISTAr LISTAS DE ESPERA
        [HttpGet]
        public ActionResult<List<ListaEsperaResponseDTO>> GetAll()
        {
            return Ok(_listaEsperaService.GettAllListaEsperas());
        }

        //BUSCAR POR ID
        [HttpGet("{id}")]
        public ActionResult<ListaEsperaResponseDTO> GetById(int id)
        {
            var lista = _listaEsperaService.GetListaEsperaById(id);

            if (lista == null)
                return NotFound("Registro no se encontro.");

            return Ok(lista);
        }

        // CREAR LISTA
        [HttpPost]
        public ActionResult<ListaEsperaResponseDTO> Crear([FromBody] ListaEsperaCreateDTO listaesperaDTO)
        {
            var listaesperacreado = _listaEsperaService.CrearListaEspera(listaesperaDTO);
            return Ok(listaesperacreado);
        }

        //ACTUALIZAR
        [HttpPut("{id}")]
        public ActionResult<ListaEsperaResponseDTO> Actualizar(int id, [FromBody] ListaEsperaCreateDTO listaesperaDTO)
        {
            var listaesperaactualizado = _listaEsperaService.ActualizarListaEspera(id, listaesperaDTO);

            if (listaesperaactualizado == null)
                return NotFound("Registro no encontrado.");

            return Ok(listaesperaactualizado);
        }

      

        //API/LISTAESPERA/1/PROMOVER/1
        [HttpPost("{listaEsperaId}/promover/{mesaId}")]
        public IActionResult PromoverAReserva(int listaEsperaId, int mesaId)
        {
            try
            {
                _listaEsperaService.PromoverAReserva(listaEsperaId, mesaId);
                return Ok("Cliente promovido a reserva correctamente.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}