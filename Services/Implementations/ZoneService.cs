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
    public class ZoneService : IZoneService
    {
        private readonly MyAppDbContext _context;

        public ZoneService(MyAppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ZoneDto>> GetAllAsync()
        {
            var zones = await _context.Zones.ToListAsync();
            return zones.Select(z => new ZoneDto
            {
                Id = z.Id,
                Name = z.Name
            });
        }

        public async Task<ZoneDto?> GetByIdAsync(int id)
        {
            var zone = await _context.Zones.FirstOrDefaultAsync(z => z.Id == id);
            if (zone == null) return null;

            return new ZoneDto
            {
                Id = zone.Id,
                Name = zone.Name
            };
        }

        public async Task<ZoneDto> CreateAsync(ZoneDto zoneDto)
        {
            var zone = new Zone
            {
                Name = zoneDto.Name,
                IsAvailable = true
            };
            await _context.Zones.AddAsync(zone);
            await _context.SaveChangesAsync();

            zoneDto.Id = zone.Id;
            return zoneDto;
        }

        public async Task<ZoneDto?> UpdateAsync(int id, ZoneDto zoneDto)
        {
            var zone = await _context.Zones.FirstOrDefaultAsync(z => z.Id == id);
            if (zone == null) return null;

            zone.Name = zoneDto.Name;
            _context.Zones.Update(zone);
            await _context.SaveChangesAsync();

            return zoneDto;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var zone = await _context.Zones.FirstOrDefaultAsync(z => z.Id == id);
            if (zone == null) return false;

            _context.Zones.Remove(zone);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
