using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestauranteAPI.DTOs;
using RestauranteAPI.Services.Interfaces;

namespace RestauranteAPI.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ITableService _tableService;
        private readonly IZoneService _zoneService;
        private readonly IReservationService _reservationService;
        private readonly IWaitingListService _waitingListService;

        public DashboardController(
            ITableService tableService,
            IZoneService zoneService,
            IReservationService reservationService,
            IWaitingListService waitingListService)
        {
            _tableService = tableService;
            _zoneService = zoneService;
            _reservationService = reservationService;
            _waitingListService = waitingListService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var tablesTask = _tableService.GetAllAsync();
            var zonesTask = _zoneService.GetAllAsync();
            var reservationsTask = _reservationService.GetByDateAsync(today);
            var waitingListTask = _waitingListService.GetAllAsync();

            await Task.WhenAll(tablesTask, zonesTask, reservationsTask, waitingListTask);

            var tables = tablesTask.Result.ToList();
            var zones = zonesTask.Result.ToList();
            var reservations = reservationsTask.Result.ToList();
            var waitingEntries = waitingListTask.Result.ToList();

            var activeReservations = reservations.Count(r => r.Status == "Confirmada");
            var pendingReservations = reservations.Count(r => r.Status == "Pendiente");

            var availableTables = tables.Count(t => t.Status == "Libre");
            var largeTables = tables.Count(t => t.Status == "Libre" && t.Capacity >= 6);

            var occupiedTables = tables.Count(t => t.Status == "Ocupada");
            var totalTables = tables.Count;
            var occupancyPercent = totalTables > 0
                ? Math.Round((double)occupiedTables / totalTables * 100, 1)
                : 0;

            var waitingListCount = waitingEntries.Count(w => w.Status == "EnEspera");

            var waitMinutes = 0.0;
            var waitingNow = waitingEntries
                .Where(w => w.Status == "EnEspera" && DateTime.TryParse(w.ArrivedAt, out _))
                .ToList();
            if (waitingNow.Count > 0)
            {
                waitMinutes = Math.Round(waitingNow
                    .Average(w => (DateTime.UtcNow - DateTime.Parse(w.ArrivedAt)).TotalMinutes), 0);
            }

            var zoneSummaries = zones.Select(z =>
            {
                var zoneTables = tables.Where(t => t.ZoneName == z.Name).ToList();
                var zoneOccupied = zoneTables.Count(t => t.Status == "Ocupada");
                return new ZoneSummaryDto
                {
                    Id = z.Id,
                    Name = z.Name,
                    TableCount = zoneTables.Count,
                    OccupancyPercent = zoneTables.Count > 0
                        ? Math.Round((double)zoneOccupied / zoneTables.Count * 100, 1)
                        : 0
                };
            }).ToList();

            var upcomingBlocks = reservations
                .Where(r => r.Status == "Confirmada" || r.Status == "Pendiente")
                .OrderBy(r => r.ReservationTime)
                .Take(10)
                .Select(r => new UpcomingBlockDto
                {
                    Time = r.ReservationTime,
                    ClientName = r.ClientName,
                    TableNumber = r.TableNumber,
                    Status = r.Status
                }).ToList();

            return Ok(new DashboardResponse
            {
                Metrics = new MetricsDto
                {
                    ActiveReservations = activeReservations,
                    PendingReservations = pendingReservations,
                    AvailableTables = availableTables,
                    LargeTablesAvailable = largeTables,
                    WaitingListCount = waitingListCount,
                    AverageWaitMinutes = waitMinutes,
                    OccupancyPercent = occupancyPercent
                },
                Zones = zoneSummaries,
                UpcomingBlocks = upcomingBlocks
            });
        }
    }
}
