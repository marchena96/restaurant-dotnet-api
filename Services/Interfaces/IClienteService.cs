using RestauranteAPI.DTOs;

namespace RestauranteAPI.Servicios.Interfaces
{
    public interface IClienteService
    {
        public List<ClienteResponseDTO> GetAllClientes();
        public ClienteResponseDTO GetClienteById(int clienteId);
        public ClienteResponseDTO CrearCliente(ClienteCreateDTO clienteDTO);
        public ClienteResponseDTO ActualizarCliente(int clienteId, ClienteCreateDTO clienteDTO);
        public void EliminarCliente(int clienteId);
    }
}