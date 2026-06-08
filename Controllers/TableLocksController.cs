using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestauranteAPI.DTOs;
using RestauranteAPI.Services.Interfaces;

namespace RestauranteAPI.Controllers
{
    [Route("api/tablelocks")]
    [ApiController]
    [Authorize]
    public class TableLocksController : ControllerBase
    {
        private readonly ILockService _lockService;

        public TableLocksController(ILockService lockService)
        {
            _lockService = lockService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var locks = await _lockService.GetAllAsync();
            return Ok(locks);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var tableLock = await _lockService.GetByIdAsync(id);
            if (tableLock == null)
                return NotFound(new { message = $"Table lock with ID {id} not found." });
            return Ok(tableLock);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLock([FromBody] TableLockDto lockDto)
        {
            try
            {
                var created = await _lockService.CreateLockAsync(lockDto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLock(int id, [FromBody] TableLockDto lockDto)
        {
            var updated = await _lockService.UpdateLockAsync(id, lockDto);
            if (updated == null)
                return NotFound(new { message = $"Table lock with ID {id} not found." });
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> ReleaseLock(int id)
        {
            var released = await _lockService.ReleaseLockAsync(id);
            if (!released)
                return NotFound(new { message = $"Table lock with ID {id} not found." });
            return NoContent();
        }
    }
}
