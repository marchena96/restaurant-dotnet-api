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
    public class TableService : ITableService
    {
        private readonly MyAppDbContext _context;

        public TableService(MyAppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TableDto>> GetAllAsync()
        {
            var tables = await _context.Tables
                .Include(t => t.Zone)
                .ToListAsync();

            var dtos = new List<TableDto>();
            foreach (var t in tables)
            {
                var status = await ComputeTableStatusAsync(t);
                dtos.Add(MapToDto(t, status));
            }
            return dtos;
        }

        public async Task<TableDto?> GetByIdAsync(int id)
        {
            var table = await _context.Tables
                .Include(t => t.Zone)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (table == null) return null;

            var status = await ComputeTableStatusAsync(table);
            return MapToDto(table, status);
        }

        public async Task<TableDto> CreateAsync(CreateTableRequest request)
        {
            var zone = await _context.Zones.FirstOrDefaultAsync(z => z.Id == request.ZoneId)
                ?? throw new ArgumentException("The specified zone does not exist.");

            if (!zone.IsAvailable)
                throw new InvalidOperationException("The zone of the table is not active.");

            var table = new Table
            {
                TableNumber = request.TableNumber,
                Capacity = request.Capacity,
                ZoneId = request.ZoneId
            };

            await _context.Tables.AddAsync(table);
            await _context.SaveChangesAsync();

            return (await GetByIdAsync(table.Id))!;
        }

        public async Task<TableDto?> UpdateAsync(int id, CreateTableRequest request)
        {
            var table = await _context.Tables
                .Include(t => t.Zone)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (table == null) return null;

            table.TableNumber = request.TableNumber;
            table.Capacity = request.Capacity;
            table.ZoneId = request.ZoneId;

            await _context.SaveChangesAsync();

            var status = await ComputeTableStatusAsync(table);
            return MapToDto(table, status);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var table = await _context.Tables.FirstOrDefaultAsync(t => t.Id == id);
            if (table == null) return false;

            _context.Tables.Remove(table);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<TableDto>> GetAvailableTablesAsync(DateOnly date, TimeOnly startTime, TimeOnly endTime)
        {
            var reservedTableIds = await _context.Reservations
                .Where(r => r.Date == date && r.StartTime < endTime && r.EndTime > startTime)
                .Select(r => r.TableId)
                .Distinct()
                .ToListAsync();

            var lockedTableIds = await _context.TableLocks
                .Where(l => l.Date == date && l.StartTime < endTime && l.EndTime > startTime)
                .Select(l => l.TableId)
                .Distinct()
                .ToListAsync();

            var availableTables = await _context.Tables
                .Include(t => t.Zone)
                .Where(t => !reservedTableIds.Contains(t.Id) && !lockedTableIds.Contains(t.Id))
                .ToListAsync();

            return availableTables.Select(t => MapToDto(t, "Libre"));
        }

        public async Task<bool> IsTableAvailableAsync(int tableId, DateOnly date, TimeOnly startTime, TimeOnly endTime)
        {
            var tableExists = await _context.Tables.AnyAsync(t => t.Id == tableId);
            if (!tableExists) return false;

            var hasReservation = await _context.Reservations.AnyAsync(r =>
                r.TableId == tableId &&
                r.Date == date &&
                r.StartTime < endTime &&
                r.EndTime > startTime);

            var hasLock = await _context.TableLocks.AnyAsync(l =>
                l.TableId == tableId &&
                l.Date == date &&
                l.StartTime < endTime &&
                l.EndTime > startTime);

            return !hasReservation && !hasLock;
        }

        public async Task<TableDto?> GetByNumberAsync(string tableNumber)
        {
            var table = await _context.Tables
                .Include(t => t.Zone)
                .FirstOrDefaultAsync(t => t.TableNumber == tableNumber);

            if (table == null) return null;

            var status = await ComputeTableStatusAsync(table);
            return MapToDto(table, status);
        }

        private async Task<string> ComputeTableStatusAsync(Models.Table table)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var now = TimeOnly.FromDateTime(DateTime.UtcNow);

            var hasLock = await _context.TableLocks.AnyAsync(l =>
                l.TableId == table.Id && l.Date == today);

            if (hasLock) return "Bloqueada";

            var isOccupied = await _context.Reservations.AnyAsync(r =>
                r.TableId == table.Id &&
                r.Date == today &&
                r.StartTime <= now &&
                r.EndTime >= now);

            if (isOccupied) return "Ocupada";

            var hasReservationToday = await _context.Reservations.AnyAsync(r =>
                r.TableId == table.Id && r.Date == today);

            if (hasReservationToday) return "Reservada";

            return "Libre";
        }

        private static TableDto MapToDto(Models.Table t, string status)
        {
            return new TableDto
            {
                Id = t.Id,
                TableNumber = t.TableNumber,
                Capacity = t.Capacity,
                Status = status,
                ZoneName = t.Zone?.Name ?? ""
            };
        }
    }
}
