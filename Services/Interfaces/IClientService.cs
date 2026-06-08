using System.Collections.Generic;
using System.Threading.Tasks;
using RestauranteAPI.DTOs;

namespace RestauranteAPI.Services.Interfaces
{
    public interface IClientService
    {
        Task<IEnumerable<ClientDto>> GetAllAsync();
        Task<ClientDto?> GetByIdAsync(int id);
        Task<ClientDto> CreateAsync(ClientDto clientDto);
        Task<ClientDto?> UpdateAsync(int id, ClientDto clientDto);
        Task<bool> DeleteAsync(int id);
    }
}
