using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestauranteAPI.DTOs;
using RestauranteAPI.Services.Interfaces;

namespace RestauranteAPI.Controllers
{
    [Route("api/turns")]
    [ApiController]
    public class TurnsController : ControllerBase
    {
        private readonly ITurnService _turnService;

        public TurnsController(ITurnService turnService)
        {
            _turnService = turnService;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var turns = await _turnService.GetAllAsync();
            return Ok(turns);
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var turn = await _turnService.GetByIdAsync(id);
            if (turn == null)
                return NotFound(new { message = $"Turn with ID {id} not found." });
            return Ok(turn);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TurnDto turnDto)
        {
            var created = await _turnService.CreateAsync(turnDto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] TurnDto turnDto)
        {
            var updated = await _turnService.UpdateAsync(id, turnDto);
            if (updated == null)
                return NotFound(new { message = $"Turn with ID {id} not found." });
            return Ok(updated);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _turnService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Turn with ID {id} not found." });
            return NoContent();
        }
    }
}