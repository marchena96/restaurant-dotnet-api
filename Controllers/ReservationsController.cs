using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestauranteAPI.DTOs;
using RestauranteAPI.Services.Interfaces;

namespace RestauranteAPI.Controllers
{
    [Route("api/reservations")]
    [ApiController]
    [Authorize]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationService _reservationService;

        public ReservationsController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var reservations = await _reservationService.GetAllAsync();
            return Ok(reservations);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var reservation = await _reservationService.GetByIdAsync(id);
            if (reservation == null)
                return NotFound(new { message = $"Reservation with ID {id} not found." });
            return Ok(reservation);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReservationRequest request)
        {
            try
            {
                var created = await _reservationService.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateReservationRequest request)
        {
            try
            {
                var updated = await _reservationService.UpdateAsync(id, request);
                if (updated == null)
                    return NotFound(new { message = $"Reservation with ID {id} not found." });
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _reservationService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Reservation with ID {id} not found." });
            return NoContent();
        }

        [HttpGet("client/{clientId}")]
        public async Task<IActionResult> GetByClient(int clientId)
        {
            var reservations = await _reservationService.GetByClientAsync(clientId);
            return Ok(reservations);
        }

        [HttpGet("date/{date}")]
        public async Task<IActionResult> GetByDate(DateOnly date)
        {
            var reservations = await _reservationService.GetByDateAsync(date);
            return Ok(reservations);
        }

        [HttpPut("{id}/status/{statusId}")]
        public async Task<IActionResult> UpdateStatus(int id, int statusId)
        {
            try
            {
                var updated = await _reservationService.UpdateStatusAsync(id, statusId);
                if (updated == null)
                    return NotFound(new { message = $"Reservation with ID {id} not found." });
                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
