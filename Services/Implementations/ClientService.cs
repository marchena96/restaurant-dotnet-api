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
    public class ClientService : IClientService
    {
        private readonly MyAppDbContext _context;

        public ClientService(MyAppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ClientDto>> GetAllAsync()
        {
            try
            {
                var clients = await _context.Clients.ToListAsync();
                return clients.Select(c => new ClientDto
                {
                    Id = c.Id,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    PhoneNumber = c.PhoneNumber,
                    IdCard = c.IdCard
                });
            }
            catch (Exception ex)
            {
                // In production, log the exception
                throw new Exception("Error retrieving clients", ex);
            }
        }

        public async Task<ClientDto?> GetByIdAsync(int id)
        {
            try
            {
                var client = await _context.Clients
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (client == null) return null;

                return new ClientDto
                {
                    Id = client.Id,
                    FirstName = client.FirstName,
                    LastName = client.LastName,
                    PhoneNumber = client.PhoneNumber,
                    IdCard = client.IdCard
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving client with ID {id}", ex);
            }
        }

        public async Task<ClientDto> CreateAsync(ClientDto clientDto)
        {
            try
            {
                // Check if Client with same IdCard already exists
                var exists = await _context.Clients.AnyAsync(c => c.IdCard == clientDto.IdCard);
                if (exists)
                {
                    throw new InvalidOperationException($"Client with ID Card {clientDto.IdCard} already exists.");
                }

                var client = new Client
                {
                    FirstName = clientDto.FirstName,
                    LastName = clientDto.LastName,
                    PhoneNumber = clientDto.PhoneNumber,
                    IdCard = clientDto.IdCard
                };

                await _context.Clients.AddAsync(client);
                await _context.SaveChangesAsync();

                clientDto.Id = client.Id;
                return clientDto;
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating client", ex);
            }
        }

        public async Task<ClientDto?> UpdateAsync(int id, ClientDto clientDto)
        {
            try
            {
                var client = await _context.Clients
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (client == null) return null;

                // Check for unique IdCard constraint
                if (client.IdCard != clientDto.IdCard)
                {
                    var exists = await _context.Clients.AnyAsync(c => c.IdCard == clientDto.IdCard && c.Id != id);
                    if (exists)
                    {
                        throw new InvalidOperationException($"Another client with ID Card {clientDto.IdCard} already exists.");
                    }
                }

                client.FirstName = clientDto.FirstName;
                client.LastName = clientDto.LastName;
                client.PhoneNumber = clientDto.PhoneNumber;
                client.IdCard = clientDto.IdCard;

                _context.Clients.Update(client);
                await _context.SaveChangesAsync();

                return clientDto;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating client with ID {id}", ex);
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var client = await _context.Clients
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (client == null) return false;

                _context.Clients.Remove(client);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting client with ID {id}", ex);
            }
        }
    }
}
