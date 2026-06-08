using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestauranteAPI.DTOs;
using RestauranteAPI.Services.Interfaces;

namespace RestauranteAPI.Controllers
{
    [Route("api/waitinglist")]
    [ApiController]
    [Authorize]
    public class WaitingListController : ControllerBase
    {
        private readonly IWaitingListService _waitingListService;

        public WaitingListController(IWaitingListService waitingListService)
        {
            _waitingListService = waitingListService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var entries = await _waitingListService.GetAllAsync();
            return Ok(entries);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var entry = await _waitingListService.GetByIdAsync(id);
            if (entry == null)
                return NotFound(new { message = $"Waiting list entry with ID {id} not found." });
            return Ok(entry);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] WaitingListDto waitingListDto)
        {
            try
            {
                var created = await _waitingListService.CreateAsync(waitingListDto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] WaitingListDto waitingListDto)
        {
            var updated = await _waitingListService.UpdateAsync(id, waitingListDto);
            if (updated == null)
                return NotFound(new { message = $"Waiting list entry with ID {id} not found." });
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _waitingListService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Waiting list entry with ID {id} not found." });
            return NoContent();
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var updated = await _waitingListService.UpdateStatusAsync(id, request.Status);
                if (updated == null)
                    return NotFound(new { message = $"Waiting list entry with ID {id} not found." });
                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/promote/{tableId}")]
        public async Task<IActionResult> PromoteToReservation(int id, int tableId)
        {
            try
            {
                var promoted = await _waitingListService.PromoteToReservationAsync(id, tableId);
                if (!promoted)
                    return BadRequest(new { message = "Could not promote waiting list entry to reservation." });
                return Ok(new { message = "Client promoted to reservation successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
