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
    public class StatusService : IStatusService
    {
        private readonly MyAppDbContext _context;

        public StatusService(MyAppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<StatusDto>> GetAllAsync()
        {
            var statuses = await _context.Statuses.ToListAsync();
            return statuses.Select(s => new StatusDto
            {
                Id = s.Id,
                Name = s.Name
            });
        }

        public async Task<StatusDto?> GetByIdAsync(int id)
        {
            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.Id == id);
            if (status == null) return null;

            return new StatusDto
            {
                Id = status.Id,
                Name = status.Name
            };
        }

        public async Task<StatusDto> CreateAsync(StatusDto statusDto)
        {
            var status = new Status
            {
                Name = statusDto.Name
            };
            await _context.Statuses.AddAsync(status);
            await _context.SaveChangesAsync();

            statusDto.Id = status.Id;
            return statusDto;
        }

        public async Task<StatusDto?> UpdateAsync(int id, StatusDto statusDto)
        {
            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.Id == id);
            if (status == null) return null;

            status.Name = statusDto.Name;
            _context.Statuses.Update(status);
            await _context.SaveChangesAsync();

            return statusDto;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.Id == id);
            if (status == null) return false;

            _context.Statuses.Remove(status);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
