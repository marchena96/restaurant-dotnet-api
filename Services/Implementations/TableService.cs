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
            var tables = await _context.Tables.ToListAsync();
            return tables.Select(t => new TableDto
            {
                Id = t.Id,
                Number = t.TableNumber,
                Capacity = t.Capacity,
                ZoneId = t.ZoneId
            });
        }

        public async Task<TableDto?> GetByIdAsync(int id)
        {
            var table = await _context.Tables.FirstOrDefaultAsync(t => t.Id == id);
            if (table == null) return null;

            return new TableDto
            {
                Id = table.Id,
                Number = table.TableNumber,
                Capacity = table.Capacity,
                ZoneId = table.ZoneId
            };
        }

        public async Task<TableDto> CreateAsync(TableDto tableDto)
        {
            var zone = await _context.Zones.FirstOrDefaultAsync(z => z.Id == tableDto.ZoneId);
            if (zone == null)
                throw new ArgumentException("The specified zone does not exist.");

            if (!zone.IsAvailable)
                throw new InvalidOperationException("The zone of the table is not active.");

            var table = new Table
            {
                TableNumber = tableDto.Number,
                Capacity = tableDto.Capacity,
                ZoneId = tableDto.ZoneId
            };

            await _context.Tables.AddAsync(table);
            await _context.SaveChangesAsync();

            tableDto.Id = table.Id;
            return tableDto;
        }

        public async Task<TableDto?> UpdateAsync(int id, TableDto tableDto)
        {
            var table = await _context.Tables.FirstOrDefaultAsync(t => t.Id == id);
            if (table == null) return null;

            table.TableNumber = tableDto.Number;
            table.Capacity = tableDto.Capacity;
            table.ZoneId = tableDto.ZoneId;

            _context.Tables.Update(table);
            await _context.SaveChangesAsync();

            return tableDto;
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
                .Where(t => !reservedTableIds.Contains(t.Id) && !lockedTableIds.Contains(t.Id))
                .ToListAsync();

            return availableTables.Select(t => new TableDto
            {
                Id = t.Id,
                Number = t.TableNumber,
                Capacity = t.Capacity,
                ZoneId = t.ZoneId
            });
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
            var table = await _context.Tables.FirstOrDefaultAsync(t => t.TableNumber == tableNumber);
            if (table == null) return null;

            return new TableDto
            {
                Id = table.Id,
                Number = table.TableNumber,
                Capacity = table.Capacity,
                ZoneId = table.ZoneId
            };
        }
    }
}
