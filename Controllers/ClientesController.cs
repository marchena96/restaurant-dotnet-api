using Microsoft.AspNetCore.Mvc;
using RestauranteAPI.DTOs;
using RestauranteAPI.Servicios.Interfaces;

namespace RestauranteAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientesController : ControllerBase
    {
        private readonly IClienteService _clienteService;

        public ClientesController(IClienteService clienteService)
        {
            _clienteService = clienteService;
        }

        //obtener todos los clientes

        [HttpGet]
        public ActionResult<List<ClienteResponseDTO>> GetClientes()
        {
            return Ok(_clienteService.GetAllClientes());
        }
        //obtener los clientes por id
        [HttpGet("{id}")]
        public ActionResult<ClienteResponseDTO> GetClienteById(int id)
        {
            var cliente = _clienteService.GetClienteById(id);

            if (cliente == null)
            {
                return NotFound("No se encontró el cliente.");
            }

            return Ok(cliente);
        }


        //crear un cliente
        [HttpPost]
        public ActionResult<ClienteResponseDTO> CrearCliente([FromBody] ClienteCreateDTO clienteDTO)
        {
            var clienteCreado = _clienteService.CrearCliente(clienteDTO);
            return Ok(clienteCreado);
        }

        //actualizar cliente
        [HttpPut("{id}")]
        public ActionResult<MesaResponseDTO> Put(int id, [FromBody] ClienteCreateDTO dto)
        {
            var result = _clienteService.ActualizarCliente(id, dto);

            if (result == null)
            {
                return NotFound("No se encontró el cliente.");
            }

            return Ok(result);
        }


        //eliminar un cliente
        [HttpDelete("{id}")]
        public IActionResult EliminarCliente(int id)
        {
            _clienteService.EliminarCliente(id);

            

            return NoContent();
        }
    }
}
