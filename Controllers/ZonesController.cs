using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestauranteAPI.DTOs;
using RestauranteAPI.Services.Interfaces;

namespace RestauranteAPI.Controllers
{
    [Route("api/zones")]
    [ApiController]
    [Authorize]
    public class ZonesController : ControllerBase
    {
        private readonly IZoneService _zoneService;

        public ZonesController(IZoneService zoneService)
        {
            _zoneService = zoneService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var zones = await _zoneService.GetAllAsync();
            return Ok(zones);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var zone = await _zoneService.GetByIdAsync(id);
            if (zone == null)
                return NotFound(new { message = $"Zone with ID {id} not found." });
            return Ok(zone);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ZoneDto zoneDto)
        {
            var created = await _zoneService.CreateAsync(zoneDto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ZoneDto zoneDto)
        {
            var updated = await _zoneService.UpdateAsync(id, zoneDto);
            if (updated == null)
                return NotFound(new { message = $"Zone with ID {id} not found." });
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _zoneService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Zone with ID {id} not found." });
            return NoContent();
        }
    }
}
