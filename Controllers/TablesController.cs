using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestauranteAPI.DTOs;
using RestauranteAPI.Services.Interfaces;

namespace RestauranteAPI.Controllers
{
    [Route("api/tables")]
    [ApiController]
    public class TablesController : ControllerBase
    {
        private readonly ITableService _tableService;
        private readonly IZoneService _zoneService;

        public TablesController(ITableService tableService, IZoneService zoneService)
        {
            _tableService = tableService;
            _zoneService = zoneService;
        }

        [AllowAnonymous]
        [HttpGet("layout")]
        public async Task<IActionResult> GetLayout()
        {
            var tables = (await _tableService.GetAllAsync()).ToList();
            var zones = (await _zoneService.GetAllAsync()).ToList();

            var zoneSummaries = zones.Select(z =>
            {
                var zoneTables = tables.Where(t => t.ZoneName == z.Name).ToList();
                var occupiedCount = zoneTables.Count(t => t.Status == "Ocupada");
                return new ZoneSummaryDto
                {
                    Id = z.Id,
                    Name = z.Name,
                    TableCount = zoneTables.Count,
                    OccupancyPercent = zoneTables.Count > 0
                        ? Math.Round((double)occupiedCount / zoneTables.Count * 100, 1)
                        : 0
                };
            });

            return Ok(new LayoutResponse
            {
                Zones = zoneSummaries,
                Tables = tables
            });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tables = await _tableService.GetAllAsync();
            return Ok(tables);
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var table = await _tableService.GetByIdAsync(id);
            if (table == null)
                return NotFound(new { message = $"Table with ID {id} not found." });
            return Ok(table);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTableRequest request)
        {
            try
            {
                var created = await _tableService.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateTableRequest request)
        {
            var updated = await _tableService.UpdateAsync(id, request);
            if (updated == null)
                return NotFound(new { message = $"Table with ID {id} not found." });
            return Ok(updated);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _tableService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Table with ID {id} not found." });
            return NoContent();
        }

        [AllowAnonymous]
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailable(
            [FromQuery] DateOnly date,
            [FromQuery] TimeOnly startTime,
            [FromQuery] TimeOnly endTime)
        {
            var tables = await _tableService.GetAvailableTablesAsync(date, startTime, endTime);
            return Ok(tables);
        }

        [AllowAnonymous]
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

        [AllowAnonymous]
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