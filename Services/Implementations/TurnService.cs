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
    public class TurnService : ITurnService
    {
        private readonly MyAppDbContext _context;

        public TurnService(MyAppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TurnDto>> GetAllAsync()
        {
            var turns = await _context.Turns.ToListAsync();
            return turns.Select(t => new TurnDto
            {
                Id = t.Id,
                Name = t.Name,
                StartTime = t.StartTime,
                EndTime = t.EndTime
            });
        }

        public async Task<TurnDto?> GetByIdAsync(int id)
        {
            var turn = await _context.Turns.FirstOrDefaultAsync(t => t.Id == id);
            if (turn == null) return null;

            return new TurnDto
            {
                Id = turn.Id,
                Name = turn.Name,
                StartTime = turn.StartTime,
                EndTime = turn.EndTime
            };
        }

        public async Task<TurnDto> CreateAsync(TurnDto turnDto)
        {
            var turn = new Turn
            {
                Name = turnDto.Name,
                StartTime = turnDto.StartTime,
                EndTime = turnDto.EndTime,
                IsActive = true
            };
            await _context.Turns.AddAsync(turn);
            await _context.SaveChangesAsync();

            turnDto.Id = turn.Id;
            return turnDto;
        }

        public async Task<TurnDto?> UpdateAsync(int id, TurnDto turnDto)
        {
            var turn = await _context.Turns.FirstOrDefaultAsync(t => t.Id == id);
            if (turn == null) return null;

            turn.Name = turnDto.Name;
            turn.StartTime = turnDto.StartTime;
            turn.EndTime = turnDto.EndTime;

            _context.Turns.Update(turn);
            await _context.SaveChangesAsync();

            return turnDto;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var turn = await _context.Turns.FirstOrDefaultAsync(t => t.Id == id);
            if (turn == null) return false;

            _context.Turns.Remove(turn);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
