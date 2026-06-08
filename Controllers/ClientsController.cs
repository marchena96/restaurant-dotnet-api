using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestauranteAPI.DTOs;
using RestauranteAPI.Services.Interfaces;

namespace RestauranteAPI.Controllers
{
    [Route("api/clients")]
    [ApiController]
    [Authorize]
    public class ClientsController : ControllerBase
    {
        private readonly IClientService _clientService;

        public ClientsController(IClientService clientService)
        {
            _clientService = clientService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var clients = await _clientService.GetAllAsync();
            return Ok(clients);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var client = await _clientService.GetByIdAsync(id);
            if (client == null)
                return NotFound(new { message = $"Client with ID {id} not found." });
            return Ok(client);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ClientDto clientDto)
        {
            try
            {
                var created = await _clientService.CreateAsync(clientDto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ClientDto clientDto)
        {
            try
            {
                var updated = await _clientService.UpdateAsync(id, clientDto);
                if (updated == null)
                    return NotFound(new { message = $"Client with ID {id} not found." });
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _clientService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Client with ID {id} not found." });
            return NoContent();
        }
    }
}
