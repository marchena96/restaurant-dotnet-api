using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestauranteAPI.DTOs;
using RestauranteAPI.Services.Interfaces;

namespace RestauranteAPI.Controllers
{
    [Route("api/statuses")]
    [ApiController]
    [Authorize]
    public class StatusesController : ControllerBase
    {
        private readonly IStatusService _statusService;

        public StatusesController(IStatusService statusService)
        {
            _statusService = statusService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var statuses = await _statusService.GetAllAsync();
            return Ok(statuses);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var status = await _statusService.GetByIdAsync(id);
            if (status == null)
                return NotFound(new { message = $"Status with ID {id} not found." });
            return Ok(status);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StatusDto statusDto)
        {
            var created = await _statusService.CreateAsync(statusDto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] StatusDto statusDto)
        {
            var updated = await _statusService.UpdateAsync(id, statusDto);
            if (updated == null)
                return NotFound(new { message = $"Status with ID {id} not found." });
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _statusService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Status with ID {id} not found." });
            return NoContent();
        }
    }
}
