using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestauranteAPI.DTOs;
using RestauranteAPI.Services.Interfaces;

namespace RestauranteAPI.Controllers
{
    [Route("api/tables")]
    [ApiController]
    [Authorize]
    public class TablesController : ControllerBase
    {
        private readonly ITableService _tableService;

        public TablesController(ITableService tableService)
        {
            _tableService = tableService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tables = await _tableService.GetAllAsync();
            return Ok(tables);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var table = await _tableService.GetByIdAsync(id);
            if (table == null)
                return NotFound(new { message = $"Table with ID {id} not found." });
            return Ok(table);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TableDto tableDto)
        {
            try
            {
                var created = await _tableService.CreateAsync(tableDto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] TableDto tableDto)
        {
            var updated = await _tableService.UpdateAsync(id, tableDto);
            if (updated == null)
                return NotFound(new { message = $"Table with ID {id} not found." });
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _tableService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Table with ID {id} not found." });
            return NoContent();
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailable(
            [FromQuery] DateOnly date,
            [FromQuery] TimeOnly startTime,
            [FromQuery] TimeOnly endTime)
        {
            var tables = await _tableService.GetAvailableTablesAsync(date, startTime, endTime);
            return Ok(tables);
        }

        [HttpGet("{id}/availability")]
        public async Task<IActionResult> CheckAvailability(
            int id,
            [FromQuery] DateOnly date,
            [FromQuery] TimeOnly startTime,
            [FromQuery] TimeOnly endTime)
        {
            var available = await _tableService.IsTableAvailableAsync(id, date, startTime, endTime);
            return Ok(new { tableId = id, isAvailable = available });
        }

        [HttpGet("number/{tableNumber}")]
        public async Task<IActionResult> GetByNumber(string tableNumber)
        {
            var table = await _tableService.GetByNumberAsync(tableNumber);
            if (table == null)
                return NotFound(new { message = $"Table with number {tableNumber} not found." });
            return Ok(table);
        }
    }
}
