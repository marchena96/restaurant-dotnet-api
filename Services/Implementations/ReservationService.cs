using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RestauranteAPI.Data;
using RestauranteAPI.DTOs;
using RestauranteAPI.Models;
using RestauranteAPI.Services.Interfaces;

namespace RestauranteAPI.Services.Implementations
{
    public class ReservationService : IReservationService
    {
        private readonly MyAppDbContext _context;

        private static readonly Dictionary<string, string> StatusMap = new()
        {
            ["Active"] = "Confirmada",
            ["Pending"] = "Pendiente",
            ["Completed"] = "Completada",
            ["Cancelled"] = "Cancelada"
        };

        public ReservationService(MyAppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ReservationDto>> GetAllAsync()
        {
            var reservations = await _context.Reservations
                .Include(r => r.Client)
                .Include(r => r.Table).ThenInclude(t => t.Zone)
                .Include(r => r.Status)
                .Include(r => r.Turn)
                .ToListAsync();

            return reservations.Select(MapToDto);
        }

        public async Task<ReservationDto?> GetByIdAsync(int id)
        {
            var r = await _context.Reservations
                .Include(r => r.Client)
                .Include(r => r.Table).ThenInclude(t => t.Zone)
                .Include(r => r.Status)
                .Include(r => r.Turn)
                .FirstOrDefaultAsync(res => res.Id == id);

            return r == null ? null : MapToDto(r);
        }

        public async Task<ReservationDto> CreateAsync(CreateReservationRequest request)
        {
            var date = DateOnly.Parse(request.Date);
            var startTime = TimeOnly.Parse(request.ReservationTime);
            var endTime = startTime.AddHours(1);

            if (startTime >= endTime)
                throw new ArgumentException("Start time must be before end time.");

            var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == request.ClientId)
                ?? throw new ArgumentException("The specified client does not exist.");

            var table = await _context.Tables
                .Include(t => t.Zone)
                .FirstOrDefaultAsync(t => t.Id == request.TableId)
                ?? throw new ArgumentException("The specified table does not exist.");

            if (request.GuestCount <= 0)
                throw new ArgumentException("Guest count must be greater than zero.");

            if (request.GuestCount > table.Capacity)
                throw new ArgumentException("Guest count exceeds table capacity.");

            if (table.Zone == null || !table.Zone.IsAvailable)
                throw new InvalidOperationException("The zone of the table is not active.");

            var validTurn = await _context.Turns.AnyAsync(t =>
                t.IsActive && startTime >= t.StartTime && endTime <= t.EndTime);

            if (!validTurn)
                throw new InvalidOperationException("Reservation time must fall inside a valid open turn.");

            var isLocked = await _context.TableLocks.AnyAsync(l =>
                l.TableId == request.TableId &&
                l.Date == date &&
                startTime < l.EndTime &&
                endTime > l.StartTime);

            if (isLocked)
            {
                await AddToWaitingListAsync(request, date, startTime, endTime);
                throw new InvalidOperationException("The table is currently locked. The client has been placed on the waiting list.");
            }

            var cancelledStatus = await _context.Statuses.FirstOrDefaultAsync(s => s.Name == "Cancelled");
            int cancelledId = cancelledStatus?.Id ?? 4;

            var isReserved = await _context.Reservations.AnyAsync(res =>
                res.TableId == request.TableId &&
                res.Date == date &&
                res.StatusId != cancelledId &&
                startTime < res.EndTime &&
                endTime > res.StartTime);

            if (isReserved)
            {
                await AddToWaitingListAsync(request, date, startTime, endTime);
                throw new InvalidOperationException("The table is already reserved. The client has been placed on the waiting list.");
            }

            var reservation = new Reservation
            {
                Date = date,
                StartTime = startTime,
                EndTime = endTime,
                GuestCount = request.GuestCount,
                ClientId = request.ClientId,
                TableId = request.TableId,
                StatusId = 2,
                TurnId = 1,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Reservations.AddAsync(reservation);
            await _context.SaveChangesAsync();

            return (await GetByIdAsync(reservation.Id))!;
        }

        public async Task<ReservationDto?> UpdateAsync(int id, CreateReservationRequest request)
        {
            var r = await _context.Reservations.FirstOrDefaultAsync(res => res.Id == id);
            if (r == null) return null;

            r.Date = DateOnly.Parse(request.Date);
            r.StartTime = TimeOnly.Parse(request.ReservationTime);
            r.EndTime = r.StartTime.AddHours(1);
            r.GuestCount = request.GuestCount;
            r.ClientId = request.ClientId;
            r.TableId = request.TableId;

            await _context.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var r = await _context.Reservations.FirstOrDefaultAsync(res => res.Id == id);
            if (r == null) return false;

            _context.Reservations.Remove(r);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<ReservationDto>> GetByClientAsync(int clientId)
        {
            var reservations = await _context.Reservations
                .Where(r => r.ClientId == clientId)
                .Include(r => r.Client)
                .Include(r => r.Table).ThenInclude(t => t.Zone)
                .Include(r => r.Status)
                .Include(r => r.Turn)
                .ToListAsync();

            return reservations.Select(MapToDto);
        }

        public async Task<IEnumerable<ReservationDto>> GetByDateAsync(DateOnly date)
        {
            var reservations = await _context.Reservations
                .Where(r => r.Date == date)
                .Include(r => r.Client)
                .Include(r => r.Table).ThenInclude(t => t.Zone)
                .Include(r => r.Status)
                .Include(r => r.Turn)
                .ToListAsync();

            return reservations.Select(MapToDto);
        }

        public async Task<ReservationDto?> UpdateStatusAsync(int id, int statusId)
        {
            var r = await _context.Reservations.FirstOrDefaultAsync(res => res.Id == id);
            if (r == null) return null;

            var statusExists = await _context.Statuses.AnyAsync(s => s.Id == statusId);
            if (!statusExists)
                throw new ArgumentException("The specified status does not exist.");

            r.StatusId = statusId;
            await _context.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        private ReservationDto MapToDto(Reservation r)
        {
            return new ReservationDto
            {
                Id = r.Id,
                Date = r.Date.ToString("yyyy-MM-dd"),
                ReservationTime = r.StartTime.ToString("HH:mm"),
                GuestCount = r.GuestCount,
                Status = StatusMap.GetValueOrDefault(r.Status?.Name ?? "", r.Status?.Name ?? ""),
                CreatedAt = r.CreatedAt.ToString("o"),
                ClientId = r.ClientId,
                ClientName = r.Client != null ? $"{r.Client.FirstName} {r.Client.LastName}" : "",
                TableId = r.TableId,
                TableNumber = r.Table?.TableNumber ?? "",
                ZoneName = r.Table?.Zone?.Name ?? "",
                TurnId = r.TurnId,
                TurnName = r.Turn?.Name ?? ""
            };
        }

        private async Task AddToWaitingListAsync(CreateReservationRequest request, DateOnly date, TimeOnly startTime, TimeOnly endTime)
        {
            var wEntry = new WaitingListEntry
            {
                ClientId = request.ClientId,
                Date = date,
                StartTime = startTime,
                EndTime = endTime,
                PartySize = request.GuestCount,
                Status = "Waiting"
            };
            await _context.WaitingLists.AddAsync(wEntry);
            await _context.SaveChangesAsync();
        }
    }
}
