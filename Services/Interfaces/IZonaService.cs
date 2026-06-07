using RestauranteAPI.DTOs;
using RestauranteAPI.Entidades;

namespace RestauranteAPI.Servicios.Interfaces
{
    public interface IZonaService
    {
        public List<ZonaResponseDTO> GetAllZonas();
        public ZonaResponseDTO GetZonaByNombre(string Nombre);
        public ZonaResponseDTO CrearZona(ZonaCreateDTO zonaDTO);
        public ZonaResponseDTO ActualizarZona(int zonaId, ZonaCreateDTO zonaDTO);
        public void EliminarZona(int zonaId);
    }
}