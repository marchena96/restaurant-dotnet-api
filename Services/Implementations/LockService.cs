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
    public class LockService : ILockService
    {
        private readonly MyAppDbContext _context;

        public LockService(MyAppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TableLockDto>> GetAllAsync()
        {
            var locks = await _context.TableLocks.ToListAsync();
            return locks.Select(l => new TableLockDto
            {
                Id = l.Id,
                TableId = l.TableId,
                Date = l.Date,
                StartTime = l.StartTime,
                EndTime = l.EndTime,
                Reason = l.Reason
            });
        }

        public async Task<TableLockDto?> GetByIdAsync(int id)
        {
            var tableLock = await _context.TableLocks.FirstOrDefaultAsync(l => l.Id == id);
            if (tableLock == null) return null;

            return new TableLockDto
            {
                Id = tableLock.Id,
                TableId = tableLock.TableId,
                Date = tableLock.Date,
                StartTime = tableLock.StartTime,
                EndTime = tableLock.EndTime,
                Reason = tableLock.Reason
            };
        }

        public async Task<TableLockDto> CreateLockAsync(TableLockDto lockDto)
        {
            var table = await _context.Tables.FirstOrDefaultAsync(t => t.Id == lockDto.TableId);
            if (table == null)
                throw new ArgumentException("The specified table does not exist.");

            if (lockDto.StartTime >= lockDto.EndTime)
                throw new ArgumentException("Start time must be before end time.");

            // Check if there is an overlapping lock
            var exists = await _context.TableLocks.AnyAsync(l =>
                l.TableId == lockDto.TableId &&
                l.Date == lockDto.Date &&
                lockDto.StartTime < l.EndTime &&
                lockDto.EndTime > l.StartTime
            );

            if (exists)
                throw new InvalidOperationException("The table is already locked during the requested time slot.");

            var tableLock = new TableLock
            {
                TableId = lockDto.TableId,
                Date = lockDto.Date,
                StartTime = lockDto.StartTime,
                EndTime = lockDto.EndTime,
                Reason = lockDto.Reason
            };

            await _context.TableLocks.AddAsync(tableLock);
            await _context.SaveChangesAsync();

            lockDto.Id = tableLock.Id;
            return lockDto;
        }

        public async Task<TableLockDto?> UpdateLockAsync(int id, TableLockDto lockDto)
        {
            var tableLock = await _context.TableLocks.FirstOrDefaultAsync(l => l.Id == id);
            if (tableLock == null) return null;

            tableLock.Date = lockDto.Date;
            tableLock.StartTime = lockDto.StartTime;
            tableLock.EndTime = lockDto.EndTime;
            tableLock.Reason = lockDto.Reason;
            tableLock.TableId = lockDto.TableId;

            _context.TableLocks.Update(tableLock);
            await _context.SaveChangesAsync();

            return lockDto;
        }

        public async Task<bool> ReleaseLockAsync(int id)
        {
            var tableLock = await _context.TableLocks.FirstOrDefaultAsync(l => l.Id == id);
            if (tableLock == null) return false;

            _context.TableLocks.Remove(tableLock);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
