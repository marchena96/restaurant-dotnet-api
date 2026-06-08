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
    public class WaitingListService : IWaitingListService
    {
        private readonly MyAppDbContext _context;

        public WaitingListService(MyAppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<WaitingListDto>> GetAllAsync()
        {
            var entries = await _context.WaitingLists.ToListAsync();
            return entries.Select(w => new WaitingListDto
            {
                Id = w.Id,
                Date = w.Date,
                StartTime = w.StartTime,
                EndTime = w.EndTime,
                Quantity = w.PartySize,
                ClientId = w.ClientId,
                Status = w.Status
            });
        }

        public async Task<WaitingListDto?> GetByIdAsync(int id)
        {
            var w = await _context.WaitingLists.FirstOrDefaultAsync(entry => entry.Id == id);
            if (w == null) return null;

            return new WaitingListDto
            {
                Id = w.Id,
                Date = w.Date,
                StartTime = w.StartTime,
                EndTime = w.EndTime,
                Quantity = w.PartySize,
                ClientId = w.ClientId,
                Status = w.Status
            };
        }

        public async Task<WaitingListDto> CreateAsync(WaitingListDto wDto)
        {
            var clientExists = await _context.Clients.AnyAsync(c => c.Id == wDto.ClientId);
            if (!clientExists)
                throw new ArgumentException("The specified client does not exist.");

            var entry = new WaitingListEntry
            {
                ClientId = wDto.ClientId,
                Date = wDto.Date,
                StartTime = wDto.StartTime,
                EndTime = wDto.EndTime,
                PartySize = wDto.Quantity,
                Status = "Waiting"
            };

            await _context.WaitingLists.AddAsync(entry);
            await _context.SaveChangesAsync();

            wDto.Id = entry.Id;
            wDto.Status = entry.Status;
            return wDto;
        }

        public async Task<WaitingListDto?> UpdateAsync(int id, WaitingListDto wDto)
        {
            var w = await _context.WaitingLists.FirstOrDefaultAsync(entry => entry.Id == id);
            if (w == null) return null;

            w.Date = wDto.Date;
            w.StartTime = wDto.StartTime;
            w.EndTime = wDto.EndTime;
            w.PartySize = wDto.Quantity;
            w.ClientId = wDto.ClientId;
            w.Status = wDto.Status;

            _context.WaitingLists.Update(w);
            await _context.SaveChangesAsync();

            return wDto;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var w = await _context.WaitingLists.FirstOrDefaultAsync(entry => entry.Id == id);
            if (w == null) return false;

            _context.WaitingLists.Remove(w);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PromoteToReservationAsync(int waitingListId, int tableId)
        {
            var entry = await _context.WaitingLists.FirstOrDefaultAsync(w => w.Id == waitingListId);
            if (entry == null)
                throw new ArgumentException($"Waiting list entry with ID {waitingListId} does not exist.");

            var table = await _context.Tables.FirstOrDefaultAsync(t => t.Id == tableId);
            if (table == null)
                throw new ArgumentException($"Table with ID {tableId} does not exist.");

            if (entry.StartTime >= entry.EndTime)
                throw new ArgumentException("Start time must be before end time.");

            var zone = await _context.Zones.FirstOrDefaultAsync(z => z.Id == table.ZoneId);
            if (zone == null || !zone.IsAvailable)
                throw new InvalidOperationException("The table zone is not active.");

            var turnValido = await _context.Turns.AnyAsync(t =>
                t.IsActive &&
                entry.StartTime >= t.StartTime &&
                entry.EndTime <= t.EndTime
            );

            if (!turnValido)
                throw new InvalidOperationException("Waiting list promotion time falls outside a valid open turn.");

            var isLocked = await _context.TableLocks.AnyAsync(l =>
                l.TableId == tableId &&
                l.Date == entry.Date &&
                entry.StartTime < l.EndTime &&
                entry.EndTime > l.StartTime
            );

            if (isLocked)
                throw new InvalidOperationException("The table has an active lock during the requested schedule.");

            var activeStatus = await _context.Statuses.FirstOrDefaultAsync(s => s.Name == "Active");
            if (activeStatus == null)
                throw new InvalidOperationException("Required 'Active' status is missing.");

            var isReserved = await _context.Reservations.AnyAsync(r =>
                r.TableId == tableId &&
                r.Date == entry.Date &&
                r.StatusId == activeStatus.Id &&
                entry.StartTime < r.EndTime &&
                entry.EndTime > r.StartTime
            );

            if (isReserved)
                throw new InvalidOperationException("The table is already reserved by an active reservation.");

            var reservation = new Reservation
            {
                ClientId = entry.ClientId,
                Date = entry.Date,
                StartTime = entry.StartTime,
                EndTime = entry.EndTime,
                GuestCount = entry.PartySize,
                TableId = tableId,
                StatusId = activeStatus.Id,
                TurnId = 1 // Default to general turn
            };

            await _context.Reservations.AddAsync(reservation);
            _context.WaitingLists.Remove(entry);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
