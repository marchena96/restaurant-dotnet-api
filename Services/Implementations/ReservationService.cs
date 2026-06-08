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

        public ReservationService(MyAppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ReservationDto>> GetAllAsync()
        {
            var reservations = await _context.Reservations.ToListAsync();
            return reservations.Select(r => new ReservationDto
            {
                Id = r.Id,
                Date = r.Date,
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                Capacity = r.GuestCount,
                ClientId = r.ClientId,
                TableId = r.TableId,
                StatusId = r.StatusId,
                TurnId = r.TurnId
            });
        }

        public async Task<ReservationDto?> GetByIdAsync(int id)
        {
            var r = await _context.Reservations.FirstOrDefaultAsync(res => res.Id == id);
            if (r == null) return null;

            return new ReservationDto
            {
                Id = r.Id,
                Date = r.Date,
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                Capacity = r.GuestCount,
                ClientId = r.ClientId,
                TableId = r.TableId,
                StatusId = r.StatusId,
                TurnId = r.TurnId
            };
        }

        public async Task<ReservationDto> CreateAsync(ReservationDto rDto)
        {
            if (rDto.StartTime >= rDto.EndTime)
                throw new ArgumentException("Start time must be before end time.");

            var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == rDto.ClientId);
            if (client == null)
                throw new ArgumentException("The specified client does not exist.");

            var table = await _context.Tables.FirstOrDefaultAsync(t => t.Id == rDto.TableId);
            if (table == null)
                throw new ArgumentException("The specified table does not exist.");

            if (rDto.Capacity <= 0)
                throw new ArgumentException("Capacity must be greater than zero.");

            if (rDto.Capacity > table.Capacity)
                throw new ArgumentException("Capacity exceeds table limit.");

            var zone = await _context.Zones.FirstOrDefaultAsync(z => z.Id == table.ZoneId);
            if (zone == null || !zone.IsAvailable)
                throw new InvalidOperationException("The zone of the table is not active.");

            var turnValido = await _context.Turns.AnyAsync(t =>
                t.IsActive &&
                rDto.StartTime >= t.StartTime &&
                rDto.EndTime <= t.EndTime
            );

            if (!turnValido)
                throw new InvalidOperationException("Reservation time slot must fall inside a valid open turn.");

            var isLocked = await _context.TableLocks.AnyAsync(l =>
                l.TableId == rDto.TableId &&
                l.Date == rDto.Date &&
                rDto.StartTime < l.EndTime &&
                rDto.EndTime > l.StartTime
            );

            if (isLocked)
            {
                await AddToWaitingListAsync(rDto);
                throw new InvalidOperationException("The table is currently locked. The client has been placed on the waiting list.");
            }

            var cancelledStatus = await _context.Statuses.FirstOrDefaultAsync(s => s.Name == "Cancelled");
            int cancelledId = cancelledStatus?.Id ?? 4;

            var isReserved = await _context.Reservations.AnyAsync(res =>
                res.TableId == rDto.TableId &&
                res.Date == rDto.Date &&
                res.StatusId != cancelledId &&
                rDto.StartTime < res.EndTime &&
                rDto.EndTime > res.StartTime
            );

            if (isReserved)
            {
                await AddToWaitingListAsync(rDto);
                throw new InvalidOperationException("The table is already reserved. The client has been placed on the waiting list.");
            }

            var reservation = new Reservation
            {
                Date = rDto.Date,
                StartTime = rDto.StartTime,
                EndTime = rDto.EndTime,
                GuestCount = rDto.Capacity,
                ClientId = rDto.ClientId,
                TableId = rDto.TableId,
                StatusId = rDto.StatusId == 0 ? 2 : rDto.StatusId, // Default to Pending (Id = 2)
                TurnId = rDto.TurnId == 0 ? 1 : rDto.TurnId // Default to General Turn (Id = 1)
            };

            await _context.Reservations.AddAsync(reservation);
            await _context.SaveChangesAsync();

            rDto.Id = reservation.Id;
            rDto.StatusId = reservation.StatusId;
            rDto.TurnId = reservation.TurnId;
            return rDto;
        }

        public async Task<ReservationDto?> UpdateAsync(int id, ReservationDto rDto)
        {
            var r = await _context.Reservations.FirstOrDefaultAsync(res => res.Id == id);
            if (r == null) return null;

            r.Date = rDto.Date;
            r.StartTime = rDto.StartTime;
            r.EndTime = rDto.EndTime;
            r.GuestCount = rDto.Capacity;
            r.ClientId = rDto.ClientId;
            r.TableId = rDto.TableId;
            r.StatusId = rDto.StatusId;
            r.TurnId = rDto.TurnId;

            _context.Reservations.Update(r);
            await _context.SaveChangesAsync();

            return rDto;
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
                .ToListAsync();

            return reservations.Select(r => new ReservationDto
            {
                Id = r.Id,
                Date = r.Date,
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                Capacity = r.GuestCount,
                ClientId = r.ClientId,
                TableId = r.TableId,
                StatusId = r.StatusId,
                TurnId = r.TurnId
            });
        }

        public async Task<IEnumerable<ReservationDto>> GetByDateAsync(DateOnly date)
        {
            var reservations = await _context.Reservations
                .Where(r => r.Date == date)
                .ToListAsync();

            return reservations.Select(r => new ReservationDto
            {
                Id = r.Id,
                Date = r.Date,
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                Capacity = r.GuestCount,
                ClientId = r.ClientId,
                TableId = r.TableId,
                StatusId = r.StatusId,
                TurnId = r.TurnId
            });
        }

        public async Task<ReservationDto?> UpdateStatusAsync(int id, int statusId)
        {
            var r = await _context.Reservations.FirstOrDefaultAsync(res => res.Id == id);
            if (r == null) return null;

            var statusExists = await _context.Statuses.AnyAsync(s => s.Id == statusId);
            if (!statusExists)
                throw new ArgumentException("The specified status does not exist.");

            r.StatusId = statusId;
            _context.Reservations.Update(r);
            await _context.SaveChangesAsync();

            return new ReservationDto
            {
                Id = r.Id,
                Date = r.Date,
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                Capacity = r.GuestCount,
                ClientId = r.ClientId,
                TableId = r.TableId,
                StatusId = r.StatusId,
                TurnId = r.TurnId
            };
        }

        private async Task AddToWaitingListAsync(ReservationDto rDto)
        {
            var wEntry = new WaitingListEntry
            {
                ClientId = rDto.ClientId,
                Date = rDto.Date,
                StartTime = rDto.StartTime,
                EndTime = rDto.EndTime,
                PartySize = rDto.Capacity,
                Status = "Waiting"
            };
            await _context.WaitingLists.AddAsync(wEntry);
            await _context.SaveChangesAsync();
        }
    }
}
