using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RestauranteAPI.DTOs;

namespace RestauranteAPI.Services.Interfaces
{
    public interface ITableService
    {
        Task<IEnumerable<TableDto>> GetAllAsync();
        Task<TableDto?> GetByIdAsync(int id);
        Task<TableDto> CreateAsync(TableDto tableDto);
        Task<TableDto?> UpdateAsync(int id, TableDto tableDto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<TableDto>> GetAvailableTablesAsync(DateOnly date, TimeOnly startTime, TimeOnly endTime);
        Task<bool> IsTableAvailableAsync(int tableId, DateOnly date, TimeOnly startTime, TimeOnly endTime);
        Task<TableDto?> GetByNumberAsync(string tableNumber);
    }
}
